using Backend.Core.Common.Exceptions;
using Backend.Core.Features.Catalogo.Entities;

namespace Backend.Core.Features.Estoque.Entities;

public class MovimentacoesEstoquesItens
{
    public int Id { get; private set; }
    public int MovimentacaoId { get; private set; }
    public decimal Quantidade { get; private set; }
    public decimal CustoUnitario { get; private set; }
    public decimal ValorTotal => Quantidade * CustoUnitario;

    public decimal? QuantidadeAnterior { get; private set; }
    public decimal? CustoMedioAnterior { get; private set; }

    public Skus Sku { get; private set; }
    public string ProdutoNome { get; private set; }
    public string UnidadeMedidaSigla { get; private set; }

    public MovimentacoesEstoquesItens(Skus sku, decimal quantidade, decimal custoUnitario, string produtoNome, string unidadeMedidaSigla)
    {
        if (sku == null)
            throw new DomainException("SKU é obrigatório para item de movimentação.");
        
        if (string.IsNullOrWhiteSpace(produtoNome))
            throw new DomainException("Nome do produto é obrigatório.");

        if (quantidade <= 0)
            throw new DomainException("Quantidade deve ser maior que zero.");

        if (custoUnitario < 0)
            throw new DomainException("Custo unitário não pode ser negativo.");

        Sku = sku;
        ProdutoNome = produtoNome;
        UnidadeMedidaSigla = unidadeMedidaSigla;
        Quantidade = quantidade;
        CustoUnitario = custoUnitario;
    }

    public MovimentacoesEstoquesItens(int id, int movimentacaoId, Skus sku, decimal quantidade, decimal custoUnitario, decimal? quantidadeAnterior = null, decimal? custoMedioAnterior = null, string produtoNome = "", string unidadeMedidaSigla = "")
        : this(sku, quantidade, custoUnitario, produtoNome, unidadeMedidaSigla)
    {
        Id = id;
        MovimentacaoId = movimentacaoId;
        QuantidadeAnterior = quantidadeAnterior;
        CustoMedioAnterior = custoMedioAnterior;
    }

    public void DefinirQuantidadesECustosAnteriores(decimal quantidade, decimal custoMedio)
    {
        QuantidadeAnterior = quantidade;
        CustoMedioAnterior = custoMedio;
    }

    public void AtualizarQuantidade(decimal quantidade)
    {
        if (quantidade <= 0)
            throw new DomainException("Quantidade deve ser maior que zero.");

        Quantidade = quantidade;
    }

    public void AtualizarCustoUnitario(decimal custoUnitario)
    {
        if (custoUnitario < 0)
            throw new DomainException("Custo unitário não pode ser negativo.");

        CustoUnitario = custoUnitario;
    }
}
