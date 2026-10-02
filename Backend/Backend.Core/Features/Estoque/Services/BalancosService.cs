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

namespace Backend.Core.Features.Estoque.Services;

public sealed class BalancosService : BaseService
{
    private readonly IBalancosRepository _balancosRepository;
    private readonly ISkusRepository _skusRepository;
    private readonly IUsuariosRepository _usuariosRepository;
    private readonly MovimentacoesEstoquesService _movimentacoesService;
    private readonly IUnitOfWork _unitOfWork;

    public BalancosService(
        IBalancosRepository balancosRepository,
        ISkusRepository skusRepository,
        IUsuariosRepository usuariosRepository,
        MovimentacoesEstoquesService movimentacoesService,
        IUnitOfWork unitOfWork)
    {
        _balancosRepository = balancosRepository;
        _skusRepository = skusRepository;
        _usuariosRepository = usuariosRepository;
        _movimentacoesService = movimentacoesService;
        _unitOfWork = unitOfWork;
    }

    public Task<ResultadoPaginado<Balancos>> ObterBalancos(int pagina = 1, int tamanhoPagina = 20)
        => _balancosRepository.ObterBalancos(pagina, tamanhoPagina);

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

            itens.Add(new BalancosItens(0, sku.Sku, sku.NomeExibicao, sku.Produto!.UnidadeMedida.Sigla, sku.Estoque, linha.QuantidadeContada));
        }

        return (itens, null);
    }

    // Fecha o balanço: cada diferença vira uma movimentação (ENTRADA para sobra, SAIDA para falta) com origem BALANCO.
    public async Task<Resultado<Balancos>> Fechar(int id)
    {
        var balanco = await _balancosRepository.ObterBalancoPorId(id);
        if (balanco == null)
            return Resultado<Balancos>.Falha(new ResultadoErro("BALANCO_INEXISTENTE", "Balanço não encontrado."));

        return await ExecuteResultAsync(async () =>
        {
            try
            {
                _unitOfWork.BeginTransaction();

                var entrada = new MovimentacoesEstoques(TipoMovimentacaoEstoque.ENTRADA, OrigemMovimentacaoEstoque.BALANCO, id, null, $"Sobra apurada no balanço #{id}", balanco.Usuario);
                var saida = new MovimentacoesEstoques(TipoMovimentacaoEstoque.SAIDA, OrigemMovimentacaoEstoque.BALANCO, id, null, $"Falta apurada no balanço #{id}", balanco.Usuario);

                foreach (var item in balanco.Itens.Where(i => i.QuantidadeContada.HasValue))
                {
                    var sku = await _skusRepository.ObterSkuPorSku(item.Sku);
                    if (sku == null) continue;

                    item.AtualizarQuantidadeSistema(sku.Estoque);
                    var diferenca = item.Diferenca!.Value;
                    if (diferenca > 0) entrada.AdicionarItem(sku, diferenca, sku.CustoMedio);
                    else if (diferenca < 0) saida.AdicionarItem(sku, -diferenca, sku.CustoMedio);
                }

                balanco.Fechar();

                foreach (var mov in new[] { entrada, saida }.Where(m => m.MovimentacoesEstoquesItens.Any()))
                {
                    var resultado = await _movimentacoesService.Registrar(mov);
                    if (!resultado.Success)
                    {
                        _unitOfWork.Rollback();
                        return Resultado<Balancos>.Falha(resultado.Errors!);
                    }
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
