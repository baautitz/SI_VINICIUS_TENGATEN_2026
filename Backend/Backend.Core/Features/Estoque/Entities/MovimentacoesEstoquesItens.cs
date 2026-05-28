using Backend.Core.Common;
using Backend.Core.Features.Catalogo.Entities;

namespace Backend.Core.Features.Estoque.Entities;

public class MovimentacoesEstoquesItens
{
    public int Id { get; private set; }
    public int MovimentacaoId { get; private set; }
    public decimal Quantidade { get; private set; }
    public decimal CustoUnitario { get; private set; }
    public decimal ValorTotal => Quantidade * CustoUnitario;

    public Skus Sku { get; private set; }

    public MovimentacoesEstoquesItens(Skus sku, decimal quantidade, decimal custoUnitario)
    {
        if (sku == null)
            throw new DomainException("SKU é obrigatório para item de movimentação.");

        if (quantidade <= 0)
            throw new DomainException("Quantidade deve ser maior que zero.");

        if (custoUnitario < 0)
            throw new DomainException("Custo unitário não pode ser negativo.");

        Sku = sku;
        Quantidade = quantidade;
        CustoUnitario = custoUnitario;
    }

    public MovimentacoesEstoquesItens(int id, int movimentacaoId, Skus sku, decimal quantidade, decimal custoUnitario)
        : this(sku, quantidade, custoUnitario)
    {
        Id = id;
        MovimentacaoId = movimentacaoId;
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
