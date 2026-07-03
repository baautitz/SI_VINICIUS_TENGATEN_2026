using Backend.Core.Common.Exceptions;
using Backend.Core.Features.Catalogo.Entities;

namespace Backend.Core.Features.Vendas.Entities;

public class VendaItens
{
    public int Id { get; set; }
    public int VendaId { get; set; }
    public decimal Quantidade { get; private set; }
    public decimal ValorUnitario { get; private set; }
    public decimal ValorDesconto { get; private set; }
    public decimal ValorTotal { get; private set; }

    public Skus Sku { get; private set; }

    protected VendaItens()
    {
        Sku = null!;
    }

    public VendaItens(decimal quantidade, decimal valorUnitario, decimal valorDesconto, Skus sku)
    {
        if (quantidade <= 0)
            throw new DomainException("Quantidade deve ser maior que zero.");

        if (valorUnitario < 0)
            throw new DomainException("Valor unitário não pode ser negativo.");

        if (valorDesconto < 0)
            throw new DomainException("Valor de desconto não pode ser negativo.");

        if (sku == null)
            throw new DomainException("SKU é obrigatório.");

        Quantidade = quantidade;
        ValorUnitario = valorUnitario;
        ValorDesconto = valorDesconto;
        Sku = sku;
        AtualizarValorTotal();
    }

    public void AtualizarQuantidade(decimal quantidade)
    {
        if (quantidade <= 0)
            throw new DomainException("Quantidade deve ser maior que zero.");

        Quantidade = quantidade;
        AtualizarValorTotal();
    }

    public void AtualizarValorUnitario(decimal valorUnitario)
    {
        if (valorUnitario < 0)
            throw new DomainException("Valor unitário não pode ser negativo.");

        ValorUnitario = valorUnitario;
        AtualizarValorTotal();
    }

    public void AtualizarValorDesconto(decimal valorDesconto)
    {
        if (valorDesconto < 0)
            throw new DomainException("Valor de desconto não pode ser negativo.");

        ValorDesconto = valorDesconto;
        AtualizarValorTotal();
    }

    public void AtualizarSku(Skus sku)
    {
        Sku = sku ?? throw new DomainException("SKU é obrigatório.");
        AtualizarValorTotal();
    }

    private void AtualizarValorTotal()
    {
        ValorTotal = Quantidade * ValorUnitario - ValorDesconto;

        if (ValorTotal < 0)
            throw new DomainException("Valor total do item não pode ser negativo.");
    }
}
