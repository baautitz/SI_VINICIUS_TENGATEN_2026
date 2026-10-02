using Backend.Core.Common.Exceptions;
using Backend.Core.Features.Catalogo.Entities;
using Backend.Core.Features.Compras.Services;

namespace Backend.Core.Features.Compras.Entities;

public class CompraItens
{
    public int Id { get; set; }
    public int CompraId { get; set; }
    public decimal Quantidade { get; private set; }
    public decimal ValorUnitario { get; private set; }
    public decimal ValorDesconto { get; private set; }
    public decimal ValorTotal { get; private set; }
    public decimal ValorRateio { get; private set; }
    public decimal CustoUnitario { get; private set; }
    // Código do produto no fornecedor (cProd); só usado para gravar o vínculo, não é persistido no item.
    public string? CodigoProdutoFornecedor { get; private set; }

    public Skus Sku { get; private set; }

    protected CompraItens() { Sku = null!; }

    public CompraItens(decimal quantidade, decimal valorUnitario, decimal valorDesconto, Skus sku, string? codigoProdutoFornecedor = null, decimal? valorBruto = null)
    {
        if (quantidade <= 0)
            throw new DomainException("Quantidade deve ser maior que zero.");
        if (valorUnitario <= 0)
            throw new DomainException("Valor unitário deve ser maior que zero.");
        if (valorDesconto < 0)
            throw new DomainException("Desconto do item não pode ser negativo.");

        Quantidade = quantidade;
        ValorUnitario = valorUnitario;
        ValorDesconto = valorDesconto;
        Sku = sku ?? throw new DomainException("SKU é obrigatório.");
        CodigoProdutoFornecedor = string.IsNullOrWhiteSpace(codigoProdutoFornecedor) ? null : codigoProdutoFornecedor.Trim();
        ValorTotal = Math.Round((valorBruto ?? quantidade * valorUnitario) - valorDesconto, 2, MidpointRounding.AwayFromZero);

        if (ValorTotal < 0)
            throw new DomainException("Valor total do item não pode ser negativo.");

        CustoUnitario = RateioCompra.CustoUnitario(quantidade, ValorTotal, 0);
    }

    public void AtualizarSku(Skus sku) => Sku = sku ?? throw new DomainException("SKU é obrigatório.");

    internal void DefinirRateio(decimal rateio)
    {
        ValorRateio = rateio;
        CustoUnitario = RateioCompra.CustoUnitario(Quantidade, ValorTotal, rateio);
    }
}
