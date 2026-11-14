using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend.Core.Common;
using Backend.Core.Common.Interfaces;
using Backend.Core.Common.Results;
using Backend.Core.Features.Acesso.Repositories;
using Backend.Core.Features.Catalogo.Repositories;
using Backend.Core.Features.Estoque.Commands;
using Backend.Core.Features.Estoque.Entities;
using Backend.Core.Features.Estoque.Entities.Enums;
using Backend.Core.Features.Estoque.Repositories;
using Backend.Core.Features.NFe.Services;

namespace Backend.Core.Features.Estoque.Services;

public sealed class BalancosService : BaseService
{
    private readonly IBalancosRepository _balancosRepository;
    private readonly ISkusRepository _skusRepository;
    private readonly IUsuariosRepository _usuariosRepository;
    private readonly MovimentacoesEstoquesService _movimentacoesService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly NfesBaixaEstoqueService _nfesBaixa;

    public BalancosService(
        IBalancosRepository balancosRepository,
        ISkusRepository skusRepository,
        IUsuariosRepository usuariosRepository,
        MovimentacoesEstoquesService movimentacoesService,
        IUnitOfWork unitOfWork,
        NfesBaixaEstoqueService nfesBaixa)
    {
        _nfesBaixa = nfesBaixa;
        _balancosRepository = balancosRepository;
        _skusRepository = skusRepository;
        _usuariosRepository = usuariosRepository;
        _movimentacoesService = movimentacoesService;
        _unitOfWork = unitOfWork;
    }

    public Task<ResultadoPaginado<Balancos>> ObterBalancos(int pagina = 1, int tamanhoPagina = 20, string? ordenarPor = null, string? direcao = null)
        => _balancosRepository.ObterBalancos(pagina, tamanhoPagina, ordenarPor, direcao);

    public Task<Balancos?> ObterBalancoPorId(int id)
        => _balancosRepository.ObterBalancoPorId(id);

    public async Task<Resultado<Balancos>> CriarBalanco(CriarBalancoCommand command)
    {
        if (command.Observacao?.Length > 500)
            return Resultado<Balancos>.Falha(new ResultadoErro("OBSERVACAO_EXCEDE_LIMITE", "Observação deve ter no máximo 500 caracteres.", "Observacao"));

        var usuario = command.UsuarioId.HasValue ? await _usuariosRepository.ObterUsuarioPorId(command.UsuarioId.Value) : null;
        if (command.UsuarioId.HasValue && usuario == null)
            return Resultado<Balancos>.Falha(new ResultadoErro("USUARIO_INEXISTENTE", "O usuário informado não existe.", "UsuarioId"));

        var (itens, erro) = await MontarItens(command.Itens);
        if (erro != null)
            return Resultado<Balancos>.Falha(erro);

        var balanco = new Balancos(command.Observacao, usuario);
        foreach (var item in itens!)
            balanco.AdicionarItem(item);

        return await ExecuteResultAsync(async () =>
        {
            try
            {
                _unitOfWork.BeginTransaction();
                var criado = await _balancosRepository.CriarBalanco(balanco);
                _unitOfWork.Commit();
                return Resultado<Balancos>.Sucesso(criado);
            }
            catch
            {
                _unitOfWork.Rollback();
                throw;
            }
        });
    }

    public async Task<Resultado<Balancos>> AtualizarBalanco(int id, AtualizarBalancoCommand command)
    {
        if (command.Observacao?.Length > 500)
            return Resultado<Balancos>.Falha(new ResultadoErro("OBSERVACAO_EXCEDE_LIMITE", "Observação deve ter no máximo 500 caracteres.", "Observacao"));

        var balanco = await _balancosRepository.ObterBalancoPorId(id);
        if (balanco == null)
            return Resultado<Balancos>.Falha(new ResultadoErro("BALANCO_INEXISTENTE", "Balanço não encontrado."));

        balanco.ExigirAberto();

        var (itens, erro) = await MontarItens(command.Itens);
        if (erro != null)
            return Resultado<Balancos>.Falha(erro);

        balanco.Atualizar(command.Observacao, itens!);

        return await ExecuteResultAsync(async () =>
        {
            try
            {
                _unitOfWork.BeginTransaction();
                await _balancosRepository.AtualizarBalanco(balanco);
                _unitOfWork.Commit();
                return Resultado<Balancos>.Sucesso((await _balancosRepository.ObterBalancoPorId(id))!);
            }
            catch
            {
                _unitOfWork.Rollback();
                throw;
            }
        });
    }

    // Itens escolhidos pelo usuário: o saldo do sistema é o vigente no momento do lançamento.
    private async Task<(List<BalancosItens>? Itens, ResultadoErro? Erro)> MontarItens(List<ContagemItemCommand>? comandos)
    {
        if (comandos == null || comandos.Count == 0)
            return (null, new ResultadoErro("ITENS_OBRIGATORIOS", "O balanço deve conter pelo menos um item.", "Itens"));

        var itens = new List<BalancosItens>();
        for (var i = 0; i < comandos.Count; i++)
        {
            var linha = comandos[i];
            var codigo = linha.Sku?.Trim().ToUpperInvariant() ?? string.Empty;

            if (linha.QuantidadeContada < 0)
                return (null, new ResultadoErro("QUANTIDADE_INVALIDA", "Quantidade contada não pode ser negativa.", $"itens.{i}.quantidadeContada"));

            if (itens.Any(x => x.Sku == codigo))
                return (null, new ResultadoErro("SKU_DUPLICADO", $"O SKU '{codigo}' foi informado mais de uma vez.", $"itens.{i}.sku"));

            var sku = await _skusRepository.ObterSkuPorSku(codigo);
            if (sku == null)
                return (null, new ResultadoErro("SKU_INEXISTENTE", $"O SKU '{codigo}' não existe.", $"itens.{i}.sku"));

            if (!sku.AceitaQuantidade(linha.QuantidadeContada))
                return (null, new ResultadoErro("QUANTIDADE_FRACIONADA", $"O SKU '{sku.Sku}' usa a unidade {sku.Produto!.UnidadeMedida.Sigla}, que não permite quantidade fracionada.", $"itens.{i}.quantidadeContada"));

            MotivoMovimentacaoEstoque? motivo = null;
            if (!string.IsNullOrWhiteSpace(linha.MotivoDivergencia))
            {
                if (!Enum.TryParse<MotivoMovimentacaoEstoque>(linha.MotivoDivergencia, true, out var parsed) || !Enum.IsDefined(parsed))
                    return (null, new ResultadoErro("MOTIVO_INVALIDO", $"Motivo da divergência inválido para o SKU '{sku.Sku}'.", $"itens.{i}.motivoDivergencia"));
                motivo = parsed;
            }

            itens.Add(new BalancosItens(0, sku.Sku, sku.NomeExibicao, sku.Produto!.UnidadeMedida.Sigla, sku.Estoque, linha.QuantidadeContada, motivo, linha.Recontado));
        }

        return (itens, null);
    }

    // Fecha o balanço (ADR 0006): toda divergência precisa estar justificada. Falta exige motivo; sobra exige recontagem e
    // motivo (erro de contagem ou outros). Cada grupo (sentido + motivo) vira uma movimentação com origem BALANCO, e, no Paraná
    // (SEFA/PR, Consulta 3/2024), toda divergência tem nota: a falta emite a NF-e de baixa (5.927) e a sobra, a entrada de ajuste de
    // inventário (1.949).
    public async Task<Resultado<Balancos>> Fechar(int id, FecharBalancoCommand? command = null)
    {
        var balanco = await _balancosRepository.ObterBalancoPorId(id);
        if (balanco == null)
            return Resultado<Balancos>.Falha(new ResultadoErro("BALANCO_INEXISTENTE", "Balanço não encontrado."));

        return await ExecuteResultAsync(async () =>
        {
            try
            {
                _unitOfWork.BeginTransaction();

                var grupos = new Dictionary<(TipoMovimentacaoEstoque Tipo, MotivoMovimentacaoEstoque Motivo), MovimentacoesEstoques>();
                var erros = new List<ResultadoErro>();

                foreach (var item in balanco.Itens.Where(i => i.QuantidadeContada.HasValue))
                {
                    var sku = await _skusRepository.ObterSkuPorSku(item.Sku);
                    if (sku == null) continue;

                    item.AtualizarQuantidadeSistema(sku.Estoque);
                    var diferenca = item.Diferenca!.Value;
                    if (diferenca == 0) continue;

                    var erro = ErroDeDivergencia(item, diferenca);
                    if (erro != null)
                    {
                        erros.Add(erro);
                        continue;
                    }

                    var tipo = diferenca > 0 ? TipoMovimentacaoEstoque.ENTRADA : TipoMovimentacaoEstoque.SAIDA;
                    var motivo = item.MotivoDivergencia!.Value;
                    if (!grupos.TryGetValue((tipo, motivo), out var movimentacao))
                    {
                        var rotulo = tipo == TipoMovimentacaoEstoque.ENTRADA ? "Sobra" : "Falta";
                        movimentacao = new MovimentacoesEstoques(tipo, OrigemMovimentacaoEstoque.BALANCO, id,
                            $"{rotulo} apurada no balanço #{id} ({motivo})", balanco.Usuario, motivo);
                        grupos[(tipo, motivo)] = movimentacao;
                    }
                    movimentacao.AdicionarItem(sku, Math.Abs(diferenca), sku.CustoMedio);
                }

                if (erros.Count == 0 && command?.EmitenteId is null && grupos.Count > 0)
                    erros.Add(new ResultadoErro("EMITENTE_OBRIGATORIO", "Informe o emitente da NF-e de ajuste de estoque.", "emitenteId"));

                if (erros.Count > 0)
                {
                    _unitOfWork.Rollback();
                    return Resultado<Balancos>.Falha(erros);
                }

                balanco.Fechar();

                foreach (var (chave, movimentacao) in grupos)
                {
                    var resultado = await _movimentacoesService.Registrar(movimentacao);
                    if (!resultado.Success)
                    {
                        _unitOfWork.Rollback();
                        return Resultado<Balancos>.Falha(resultado.Errors!);
                    }

                    if (chave.Tipo == TipoMovimentacaoEstoque.SAIDA)
                        await _nfesBaixa.EmitirBaixa(resultado.Data!, command!.EmitenteId!.Value);
                    else
                        await _nfesBaixa.EmitirEntradaDeAjuste(resultado.Data!, command!.EmitenteId!.Value);
                }

                await _balancosRepository.AtualizarBalanco(balanco);
                _unitOfWork.Commit();
                return Resultado<Balancos>.Sucesso(balanco);
            }
            catch
            {
                _unitOfWork.Rollback();
                throw;
            }
        });
    }

    // Regra da divergência: falta pede um dos quatro motivos de perda; sobra pede recontagem e motivo de contagem ou outros.
    private static ResultadoErro? ErroDeDivergencia(BalancosItens item, decimal diferenca)
    {
        const string campo = "itens";
        if (item.MotivoDivergencia is null)
            return new ResultadoErro("MOTIVO_DIVERGENCIA_OBRIGATORIO",
                $"SKU '{item.Sku}': informe o motivo {(diferenca < 0 ? "da falta" : "da sobra")}.", campo);

        if (diferenca < 0)
            return item.MotivoDivergencia == MotivoMovimentacaoEstoque.OUTROS
                ? new ResultadoErro("MOTIVO_FALTA_INVALIDO",
                    $"SKU '{item.Sku}': falta só pode ter motivo perda/avaria, furto/roubo, vencimento ou erro de contagem.", campo)
                : null;

        if (item.MotivoDivergencia is not (MotivoMovimentacaoEstoque.ERRO_DE_CONTAGEM or MotivoMovimentacaoEstoque.OUTROS))
            return new ResultadoErro("MOTIVO_SOBRA_INVALIDO",
                $"SKU '{item.Sku}': sobra só pode ter motivo erro de contagem ou outros.", campo);

        return item.Recontado
            ? null
            : new ResultadoErro("RECONTAGEM_OBRIGATORIA", $"SKU '{item.Sku}': sobra exige recontagem antes de fechar o balanço.", campo);
    }

    // Aberto: só descarta. Fechado: estorna as movimentações geradas (exige motivo) e cancela.
    public async Task<Resultado<Balancos>> Cancelar(int id, CancelarBalancoCommand? command)
    {
        var balanco = await _balancosRepository.ObterBalancoPorId(id);
        if (balanco == null)
            return Resultado<Balancos>.Falha(new ResultadoErro("BALANCO_INEXISTENTE", "Balanço não encontrado."));

        var fechado = balanco.Status == StatusBalanco.FECHADO;
        var motivo = command?.Motivo?.Trim();
        if (fechado && (string.IsNullOrEmpty(motivo) || motivo.Length < 5 || motivo.Length > 500))
            return Resultado<Balancos>.Falha(new ResultadoErro("MOTIVO_INVALIDO", "Motivo do cancelamento deve ter entre 5 e 500 caracteres.", "motivo"));

        return await ExecuteResultAsync(async () =>
        {
            try
            {
                _unitOfWork.BeginTransaction();
                if (fechado)
                    await _movimentacoesService.EstornarPorOrigem(OrigemMovimentacaoEstoque.BALANCO, id, motivo!);

                balanco.Cancelar();
                await _balancosRepository.AtualizarBalanco(balanco);
                _unitOfWork.Commit();
                return Resultado<Balancos>.Sucesso(balanco);
            }
            catch
            {
                _unitOfWork.Rollback();
                throw;
            }
        });
    }
}
