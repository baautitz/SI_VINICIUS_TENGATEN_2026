using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend.Core.Common;
using Backend.Core.Common.Exceptions;
using Backend.Core.Common.Extensions;
using Backend.Core.Common.Results;
using Backend.Core.Common.Interfaces;
using Backend.Core.Features.Acesso.Repositories;
using Backend.Core.Features.Catalogo.Entities;
using Backend.Core.Features.Catalogo.Repositories;
using Backend.Core.Features.Estoque.Commands;
using Backend.Core.Features.Estoque.Entities;
using Backend.Core.Features.Estoque.Entities.Enums;
using Backend.Core.Features.Estoque.Repositories;
using Backend.Core.Features.Estoque.Validators.Commands;
using Backend.Core.Features.NFe.Services;

namespace Backend.Core.Features.Estoque.Services;

public sealed class MovimentacoesEstoquesService : BaseService
{
    private readonly IMovimentacoesEstoquesRepository _movimentacoesRepository;
    private readonly ISkusRepository _skusRepository;
    private readonly IUsuariosRepository _usuariosRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly NfesBaixaEstoqueService? _nfesBaixa;

    public MovimentacoesEstoquesService(
        IMovimentacoesEstoquesRepository movimentacoesRepository,
        ISkusRepository skusRepository,
        IUsuariosRepository usuariosRepository,
        IUnitOfWork unitOfWork,
        NfesBaixaEstoqueService? nfesBaixa = null)
    {
        _nfesBaixa = nfesBaixa;
        _movimentacoesRepository = movimentacoesRepository;
        _skusRepository = skusRepository;
        _usuariosRepository = usuariosRepository;
        _unitOfWork = unitOfWork;
    }

    public Task<ResultadoPaginado<MovimentacoesEstoques>> ObterMovimentacoes(string? search, int pagina = 1, int tamanhoPagina = 20)
        => string.IsNullOrWhiteSpace(search)
            ? _movimentacoesRepository.ObterMovimentacoes(pagina, tamanhoPagina)
            : _movimentacoesRepository.PesquisarMovimentacoes(search, pagina, tamanhoPagina);

    public async Task<ResultadoPaginado<MovimentacoesEstoques>> ObterMovimentacoesPorOrigem(OrigemMovimentacaoEstoque origemTipo, int origemId)
    {
        var lista = await _movimentacoesRepository.ObterMovimentacoesPorOrigem(origemTipo, origemId);
        return new ResultadoPaginado<MovimentacoesEstoques>(lista, lista.Count, 1, Math.Max(lista.Count, 1));
    }

    public Task<MovimentacoesEstoques?> ObterMovimentacaoPorId(int id)
        => _movimentacoesRepository.ObterMovimentacaoPorId(id);

    // Lançamento manual: sempre saída, com motivo tipado (ADR 0006). Perda/avaria, furto/roubo e vencimento emitem NF-e de baixa
    // na mesma transação. Efetiva na hora; correção só por estorno.
    public async Task<Resultado<MovimentacoesEstoques>> CriarMovimentacao(CriarMovimentacaoCommand command)
    {
        var validation = new CriarMovimentacaoCommandValidator().Validate(command);
        if (!validation.IsValid)
            return Resultado<MovimentacoesEstoques>.Falha(validation.ToResultadoErros());

        var usuario = command.UsuarioId.HasValue ? await _usuariosRepository.ObterUsuarioPorId(command.UsuarioId.Value) : null;
        if (command.UsuarioId.HasValue && usuario == null)
            return Resultado<MovimentacoesEstoques>.Falha(new ResultadoErro("USUARIO_INEXISTENTE", "O usuário informado não existe.", "UsuarioId"));

        Enum.TryParse<MotivoMovimentacaoEstoque>(command.MotivoTipo, true, out var motivoTipo);
        var movimentacao = new MovimentacoesEstoques(TipoMovimentacaoEstoque.SAIDA, OrigemMovimentacaoEstoque.MANUAL, null, command.Motivo, usuario, motivoTipo);

        foreach (var itemCommand in command.Itens)
        {
            var sku = await _skusRepository.ObterSkuPorSku(itemCommand.Sku);
            if (sku == null)
                return Resultado<MovimentacoesEstoques>.Falha(new ResultadoErro("SKU_INEXISTENTE", $"O SKU '{itemCommand.Sku}' não existe.", "Itens"));

            movimentacao.AdicionarItem(sku, itemCommand.Quantidade, itemCommand.CustoUnitario ?? sku.CustoMedio);
        }

        return await ExecutarEmTransacao(movimentacao, null,
            motivoTipo.GeraNfeDeBaixa() ? registrada => EmitirBaixa(registrada, command.EmitenteId!.Value) : null);
    }

    public async Task<Resultado<MovimentacoesEstoques>> Estornar(int id, EstornarMovimentacaoCommand command)
    {
        var motivo = command?.Motivo?.Trim();
        if (string.IsNullOrEmpty(motivo))
            return Resultado<MovimentacoesEstoques>.Falha(new ResultadoErro("MOTIVO_OBRIGATORIO", "Motivo do estorno é obrigatório.", "motivo"));
        if (motivo.Length < 5 || motivo.Length > 500)
            return Resultado<MovimentacoesEstoques>.Falha(new ResultadoErro("MOTIVO_INVALIDO", "Motivo do estorno deve ter entre 5 e 500 caracteres.", "motivo"));

        var original = await _movimentacoesRepository.ObterMovimentacaoPorId(id);
        if (original == null)
            return Resultado<MovimentacoesEstoques>.Falha(new ResultadoErro("MOVIMENTACAO_INEXISTENTE", "Movimentação não encontrada."));

        if (original.OrigemTipo == OrigemMovimentacaoEstoque.VENDA)
            return Resultado<MovimentacoesEstoques>.Falha(new ResultadoErro("MOVIMENTACAO_ORIGEM_VENDA", "Esta movimentação foi gerada por uma venda. Use a devolução da venda para reentrar o estoque."));

        if (original.OrigemTipo == OrigemMovimentacaoEstoque.COMPRA)
            return Resultado<MovimentacoesEstoques>.Falha(new ResultadoErro("MOVIMENTACAO_ORIGEM_COMPRA", "Esta movimentação foi gerada por uma compra. Para desfazê-la, estorne a compra."));

        if (original.OrigemTipo == OrigemMovimentacaoEstoque.BALANCO)
            return Resultado<MovimentacoesEstoques>.Falha(new ResultadoErro("MOVIMENTACAO_ORIGEM_BALANCO", "Esta movimentação foi gerada por um balanço. Cancele o balanço para estornar a movimentação de estoque."));

        if (original.OrigemTipo == OrigemMovimentacaoEstoque.DEVOLUCAO_VENDA)
            return Resultado<MovimentacoesEstoques>.Falha(new ResultadoErro("MOVIMENTACAO_ORIGEM_DEVOLUCAO", "Esta movimentação foi gerada por uma devolução de venda e não pode ser estornada."));

        if (original.OrigemTipo == OrigemMovimentacaoEstoque.DEVOLUCAO_COMPRA)
            return Resultado<MovimentacoesEstoques>.Falha(new ResultadoErro("MOVIMENTACAO_ORIGEM_DEVOLUCAO_COMPRA", "Esta movimentação foi gerada por uma devolução de compra e não pode ser estornada."));

        if (original.OrigemTipo == OrigemMovimentacaoEstoque.ESTORNO)
            return Resultado<MovimentacoesEstoques>.Falha(new ResultadoErro("MOVIMENTACAO_ORIGEM_ESTORNO", "Um estorno não pode ser estornado. Lance uma nova movimentação."));

        if (original.Estornada)
            return Resultado<MovimentacoesEstoques>.Falha(new ResultadoErro("MOVIMENTACAO_JA_ESTORNADA", "Esta movimentação já foi estornada."));

        var (estorno, custosARestaurar) = MontarEstorno(original, motivo);
        // O estorno de uma baixa com NF-e emite a nota de estorno da baixa na mesma transação.
        return await ExecutarEmTransacao(estorno, custosARestaurar, _ => EmitirEstornoDaBaixa(original));
    }

    private async Task EmitirBaixa(MovimentacoesEstoques registrada, int emitenteId)
    {
        if (_nfesBaixa is null)
            throw new DomainException("A emissão da NF-e de baixa não está disponível.");
        await _nfesBaixa.EmitirBaixa(registrada, emitenteId);
    }

    private Task EmitirEstornoDaBaixa(MovimentacoesEstoques original)
        => _nfesBaixa is null ? Task.CompletedTask : _nfesBaixa.EmitirEstornoDaBaixa(original);

    // O estorno só devolve o custo médio de antes da entrada se ela foi a última movimentação do SKU, e só
    // zera o saldo se o item ainda está em estoque. Registrar() não confere a ordem (só impede saldo negativo),
    // então quem estorna uma entrada de documento deve chamar isto antes. Lista o que impede, vazio se puder.
    public async Task<IReadOnlyList<ResultadoErro>> ObterBloqueiosDeEstornoDeEntrada(OrigemMovimentacaoEstoque origemTipo, int origemId)
    {
        var erros = new List<ResultadoErro>();
        foreach (var entrada in (await _movimentacoesRepository.ObterMovimentacoesPorOrigem(origemTipo, origemId)).Where(m => !m.Estornada))
        {
            var posteriores = await _movimentacoesRepository.ObterMovimentacoesPosteriores(
                entrada.Id, entrada.MovimentacoesEstoquesItens.Select(i => i.Sku.Sku).ToList());

            foreach (var item in entrada.MovimentacoesEstoquesItens)
            {
                var sku = await _skusRepository.ObterSkuPorSku(item.Sku.Sku);
                if (sku is not null && sku.Estoque < item.Quantidade)
                    erros.Add(new ResultadoErro("ESTORNO_ESTOQUE_INSUFICIENTE",
                        $"SKU '{item.Sku.Sku}': estoque atual ({sku.Estoque:0.####}) menor que a quantidade da entrada ({item.Quantidade:0.####}); parte já foi vendida ou baixada. {OrientacaoEstoqueInsuficiente(origemTipo)}", "Itens"));
                else if (posteriores.Where(p => p.Sku == item.Sku.Sku).ToList() is { Count: > 0 } lista)
                    erros.Add(new ResultadoErro("ESTORNO_MOVIMENTACAO_POSTERIOR",
                        $"SKU '{item.Sku.Sku}': há movimentações posteriores à entrada ({string.Join(", ", lista.Select(p => $"#{p.MovimentacaoId} {p.Origem}"))}); estornar agora tornaria o custo médio incorreto.", "Itens"));
            }
        }
        return erros;
    }

    private static string OrientacaoEstoqueInsuficiente(OrigemMovimentacaoEstoque origemTipo) =>
        origemTipo == OrigemMovimentacaoEstoque.DEVOLUCAO_VENDA
            ? "Os itens da devolução da venda já saíram do estoque; não é possível estornar a devolução."
            : "O caminho é a devolução ao fornecedor.";

    // Usado ao cancelar o documento de origem (balanço, compra): estorna todas as movimentações dele
    // que ainda não foram estornadas. O chamador controla a transação.
    public async Task EstornarPorOrigem(OrigemMovimentacaoEstoque origemTipo, int origemId, string motivo)
    {
        foreach (var original in await _movimentacoesRepository.ObterMovimentacoesPorOrigem(origemTipo, origemId))
        {
            if (original.Estornada)
                continue;

            var (estorno, custosARestaurar) = MontarEstorno(original, motivo);
            var resultado = await Registrar(estorno, custosARestaurar);
            if (!resultado.Success)
                throw new DomainException(resultado.Errors!.First().Message);
            await EmitirEstornoDaBaixa(original);
        }
    }

    // Aplica a movimentação ao saldo dos SKUs e grava no razão. O chamador controla a transação.
    public async Task<Resultado<MovimentacoesEstoques>> Registrar(MovimentacoesEstoques movimentacao, IReadOnlyDictionary<string, (decimal CustoMedio, decimal? CustoUltimaCompra)>? custosARestaurar = null)
    {
        var skus = new Dictionary<string, Skus>();
        foreach (var item in movimentacao.MovimentacoesEstoquesItens)
        {
            var sku = await _skusRepository.ObterSkuPorSku(item.Sku.Sku);
            if (sku == null)
                return Resultado<MovimentacoesEstoques>.Falha(new ResultadoErro("SKU_INEXISTENTE", $"O SKU '{item.Sku.Sku}' não existe.", "Itens"));

            if (movimentacao.TipoMovimentacao == TipoMovimentacaoEstoque.SAIDA && sku.Estoque < item.Quantidade)
                return Resultado<MovimentacoesEstoques>.Falha(new ResultadoErro("ESTOQUE_INSUFICIENTE", $"Estoque insuficiente para o SKU '{sku.Sku}'. Disponível: {sku.Estoque:0.####}, Solicitado: {item.Quantidade:0.####}", "Itens"));

            skus[sku.Sku] = sku;
        }

        foreach (var item in movimentacao.MovimentacoesEstoquesItens)
        {
            var sku = skus[item.Sku.Sku];
            item.DefinirQuantidadesECustosAnteriores(sku.Estoque, sku.CustoMedio, sku.CustoUltimaCompra);

            if (movimentacao.TipoMovimentacao == TipoMovimentacaoEstoque.ENTRADA)
            {
                sku.RegistrarEntradaDeEstoque(item.Quantidade, item.CustoUnitario, movimentacao.OrigemTipo == OrigemMovimentacaoEstoque.COMPRA);
            }
            else if (movimentacao.OrigemTipo == OrigemMovimentacaoEstoque.DEVOLUCAO_COMPRA)
            {
                // Devolução ao fornecedor sai pelo custo da compra, que também é o que se desfaz do custo médio.
                sku.RemoverEntradaDeCompra(item.Quantidade, item.CustoUnitario);
            }
            else
            {
                // Saída é valorizada ao custo médio vigente.
                item.AtualizarCustoUnitario(sku.CustoMedio);
                if (custosARestaurar != null && custosARestaurar.TryGetValue(sku.Sku, out var custos))
                    sku.ReverterEntradaDeEstoque(item.Quantidade, custos.CustoMedio, custos.CustoUltimaCompra);
                else
                    sku.AjustarEstoque(-item.Quantidade);
            }

            await _skusRepository.AtualizarSku(sku.Sku, sku);
        }

        return Resultado<MovimentacoesEstoques>.Sucesso(await _movimentacoesRepository.CriarMovimentacao(movimentacao));
    }

    // Estorno = movimentação inversa apontando para a original.
    private static (MovimentacoesEstoques Estorno, Dictionary<string, (decimal CustoMedio, decimal? CustoUltimaCompra)>? CustosARestaurar) MontarEstorno(MovimentacoesEstoques original, string motivo)
    {
        var inverso = original.TipoMovimentacao == TipoMovimentacaoEstoque.ENTRADA ? TipoMovimentacaoEstoque.SAIDA : TipoMovimentacaoEstoque.ENTRADA;
        // Movimentações anteriores à migração não têm custo_ultima_compra_anterior: o custo da última compra fica inalterado e isso é registrado no motivo.
        var semUltimaCompra = original.TipoMovimentacao == TipoMovimentacaoEstoque.ENTRADA && original.MovimentacoesEstoquesItens.Any(i => i.CustoUltimaCompraAnterior is null);
        var estorno = new MovimentacoesEstoques(inverso, OrigemMovimentacaoEstoque.ESTORNO, original.Id,
            motivo + (semUltimaCompra ? " (custo da última compra anterior não registrado: mantido inalterado)" : ""));

        foreach (var item in original.MovimentacoesEstoquesItens)
            estorno.AdicionarItem(item.Sku, item.Quantidade, item.CustoUnitario);

        // Reverter uma entrada devolve também o custo médio de antes dela.
        var custos = original.TipoMovimentacao == TipoMovimentacaoEstoque.ENTRADA
            ? original.MovimentacoesEstoquesItens.ToDictionary(i => i.Sku.Sku, i => (
                i.CustoMedioAnterior ?? throw new DomainException($"Movimentação #{original.Id}: custo médio anterior do SKU '{i.Sku.Sku}' não foi registrado; não é possível reverter o custo."),
                i.CustoUltimaCompraAnterior))
            : null;

        return (estorno, custos);
    }

    // `aposRegistrar` roda dentro da transação, depois que a movimentação foi gravada (NF-e de baixa e de estorno da baixa).
    private async Task<Resultado<MovimentacoesEstoques>> ExecutarEmTransacao(MovimentacoesEstoques movimentacao, IReadOnlyDictionary<string, (decimal CustoMedio, decimal? CustoUltimaCompra)>? custosARestaurar,
        Func<MovimentacoesEstoques, Task>? aposRegistrar = null)
    {
        return await ExecuteResultAsync(async () =>
        {
            try
            {
                _unitOfWork.BeginTransaction();
                var resultado = await Registrar(movimentacao, custosARestaurar);
                if (resultado.Success && aposRegistrar is not null) await aposRegistrar(resultado.Data!);
                if (resultado.Success) _unitOfWork.Commit(); else _unitOfWork.Rollback();
                return resultado;
            }
            catch
            {
                _unitOfWork.Rollback();
                throw;
            }
        });
    }
}
