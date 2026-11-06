using Backend.Core.Common;
using Backend.Core.Common.Interfaces;
using Backend.Core.Common.Results;
using Backend.Core.Features.Catalogo.Repositories;
using Backend.Core.Features.Compras.Commands;
using Backend.Core.Features.Compras.Entities;
using Backend.Core.Features.Compras.Repositories;
using Backend.Core.Features.Estoque.Entities;
using Backend.Core.Features.Estoque.Entities.Enums;
using Backend.Core.Features.Estoque.Services;

namespace Backend.Core.Features.Compras.Services;

// Fluxo financeiro (crédito/abatimento no que se deve ao fornecedor) fica fora desta etapa.
public sealed class DevolucoesCompraService : BaseService
{
    public const string CompraInexistente = "COMPRA_INEXISTENTE";
    public const string DevolucaoInexistente = "DEVOLUCAO_INEXISTENTE";

    private readonly IComprasRepository _comprasRepository;
    private readonly IDevolucoesCompraRepository _devolucoesRepository;
    private readonly ISkusRepository _skusRepository;
    private readonly MovimentacoesEstoquesService _estoqueService;
    private readonly IUnitOfWork _unitOfWork;

    public DevolucoesCompraService(
        IComprasRepository comprasRepository,
        IDevolucoesCompraRepository devolucoesRepository,
        ISkusRepository skusRepository,
        MovimentacoesEstoquesService estoqueService,
        IUnitOfWork unitOfWork)
    {
        _comprasRepository = comprasRepository;
        _devolucoesRepository = devolucoesRepository;
        _skusRepository = skusRepository;
        _estoqueService = estoqueService;
        _unitOfWork = unitOfWork;
    }

    public Task<DevolucaoCompra?> ObterDevolucaoPorId(int id) => _devolucoesRepository.ObterDevolucaoPorId(id);

    public Task<IReadOnlyList<DevolucaoCompra>> ObterDevolucoesPorCompra(int compraId)
        => _devolucoesRepository.ObterDevolucoesPorCompra(compraId);

    // Saída de estoque pelo custo da compra, que também sai do custo médio. Tudo numa transação: com qualquer
    // recusa nada é gravado. Os SKUs são travados antes de conferir saldo e estoque, então devoluções
    // e vendas concorrentes esperam.
    public async Task<Resultado<DevolucaoCompra>> CriarDevolucao(int compraId, CriarDevolucaoCompraCommand command)
    {
        var motivo = command?.Motivo?.Trim() ?? "";
        if (motivo.Length < 5 || motivo.Length > 255)
            return Resultado<DevolucaoCompra>.Falha(new ResultadoErro("MOTIVO_INVALIDO", "O motivo da devolução deve ter entre 5 e 255 caracteres.", "Motivo"));

        var pedidos = (command!.Itens ?? Array.Empty<CriarDevolucaoCompraItemCommand>())
            .Where(i => i.Quantidade != 0).GroupBy(i => i.CompraItemId)
            .Select(g => new CriarDevolucaoCompraItemCommand(g.Key, g.Sum(i => i.Quantidade))).ToList();
        if (pedidos.Count == 0)
            return Resultado<DevolucaoCompra>.Falha(new ResultadoErro("ITENS_OBRIGATORIOS", "Informe ao menos um item com quantidade maior que zero.", "Itens"));
        if (pedidos.Any(i => i.Quantidade < 0))
            return Resultado<DevolucaoCompra>.Falha(new ResultadoErro("QUANTIDADE_INVALIDA", "A quantidade devolvida não pode ser negativa.", "Itens"));

        var compra = await _comprasRepository.ObterCompraPorId(compraId);
        if (compra is null)
            return Resultado<DevolucaoCompra>.Falha(new ResultadoErro(CompraInexistente, "Compra não encontrada."));

        return await ExecuteResultAsync(async () =>
        {
            try
            {
                _unitOfWork.BeginTransaction();
                var resultado = await CriarNaTransacao(compra, motivo, pedidos);
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

    private async Task<Resultado<DevolucaoCompra>> CriarNaTransacao(Compra compra, string motivo, List<CriarDevolucaoCompraItemCommand> pedidos)
    {
        await _skusRepository.TravarSkus(compra.Itens.Select(i => i.Sku.Sku).Distinct().OrderBy(s => s));

        // Relê já sob a trava: o saldo devolvível reflete devoluções concorrentes.
        compra = await _comprasRepository.ObterCompraPorId(compra.Id) ?? compra;
        if (compra.Estornada)
            return Resultado<DevolucaoCompra>.Falha(new ResultadoErro("COMPRA_ESTORNADA", "Esta compra foi estornada e não aceita devolução.", "CompraId"));

        var itens = new List<DevolucaoCompraItens>();
        var erros = new List<ResultadoErro>();
        foreach (var pedido in pedidos)
        {
            var itemCompra = compra.Itens.FirstOrDefault(i => i.Id == pedido.CompraItemId);
            if (itemCompra is null)
            {
                erros.Add(new ResultadoErro("ITEM_INEXISTENTE", $"O item {pedido.CompraItemId} não pertence à compra {compra.Id}.", "Itens"));
                continue;
            }
            if (pedido.Quantidade > itemCompra.SaldoDevolvivel)
            {
                erros.Add(new ResultadoErro("DEVOLUCAO_ACIMA_DO_SALDO",
                    $"Quantidade devolvida do SKU '{itemCompra.Sku.Sku}' excede o saldo devolvível da compra ({itemCompra.SaldoDevolvivel:0.####}).", "Itens"));
                continue;
            }
            itens.Add(new DevolucaoCompraItens(itemCompra, pedido.Quantidade));
        }
        if (erros.Count > 0)
            return Resultado<DevolucaoCompra>.Falha(erros);

        // O que já foi vendido não volta ao fornecedor: o limite é o saldo atual do SKU.
        var skus = new Dictionary<string, Catalogo.Entities.Skus>();
        foreach (var grupo in itens.GroupBy(i => i.Sku))
        {
            var sku = await _skusRepository.ObterSkuPorSku(grupo.Key);
            if (sku is null)
            {
                erros.Add(new ResultadoErro("SKU_INEXISTENTE", $"O SKU '{grupo.Key}' não existe.", "Itens"));
                continue;
            }
            skus[sku.Sku] = sku;
            var quantidade = grupo.Sum(i => i.Quantidade);
            if (quantidade > sku.Estoque)
                erros.Add(new ResultadoErro("DEVOLUCAO_ACIMA_DO_ESTOQUE",
                    $"SKU '{sku.Sku}': só há {sku.Estoque:0.####} em estoque e a devolução pede {quantidade:0.####}; o que já foi vendido ou baixado não pode ser devolvido ao fornecedor.", "Itens"));
        }
        if (erros.Count > 0)
            return Resultado<DevolucaoCompra>.Falha(erros);

        var criada = await _devolucoesRepository.CriarDevolucao(new DevolucaoCompra(compra, motivo, itens));

        var saida = new MovimentacoesEstoques(
            TipoMovimentacaoEstoque.SAIDA, OrigemMovimentacaoEstoque.DEVOLUCAO_COMPRA, criada.Id,
            observacao: $"Devolução nº {criada.Id} da compra nº {compra.Id}");
        foreach (var item in criada.Itens)
            saida.AdicionarItem(skus[item.Sku], item.Quantidade, item.CustoUnitario);

        var estoque = await _estoqueService.Registrar(saida);
        if (!estoque.Success)
            return Resultado<DevolucaoCompra>.Falha(estoque.Errors!);

        return Resultado<DevolucaoCompra>.Sucesso(criada);
    }
}
