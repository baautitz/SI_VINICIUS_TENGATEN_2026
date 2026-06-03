using Backend.Core.Common.Exceptions;
using Backend.Core.Common.Helpers;
using Backend.Core.Features.Catalogo.Entities;

namespace Backend.Core.Features.NFe.Entities;

public class NfesItens
{
    public int Id { get; set; }
    public int NfeId { get; set; }
    public int NumeroItem { get; private set; }
    public string DescricaoItem { get; private set; }
    public decimal Quantidade { get; private set; }
    public decimal ValorUnitario { get; private set; }
    public decimal ValorDesconto { get; private set; }
    public decimal ValorTotal { get; private set; }

    public Skus Sku { get; private set; }
    public UnidadesMedida UnidadeMedida { get; private set; }

    public NfesItens(
        int numeroItem,
        string descricaoItem,
        decimal quantidade,
        decimal valorUnitario,
        decimal valorDesconto,
        Skus sku,
        UnidadesMedida unidadeMedida)
    {
        if (numeroItem <= 0)
            throw new DomainException("Número do item é obrigatório.");

        descricaoItem = TextNormalization.Normalize(descricaoItem);

        if (string.IsNullOrWhiteSpace(descricaoItem))
            throw new DomainException("Descrição do item é obrigatória.");

        if (quantidade <= 0)
            throw new DomainException("Quantidade deve ser maior que zero.");

        if (valorUnitario < 0)
            throw new DomainException("Valor unitário não pode ser negativo.");

        if (valorDesconto < 0)
            throw new DomainException("Valor do desconto não pode ser negativo.");

        if (valorDesconto > quantidade * valorUnitario)
            throw new DomainException("Desconto não pode ser maior que o valor total do item.");

        Sku = sku ?? throw new DomainException("SKU é obrigatório.");
        UnidadeMedida = unidadeMedida ?? throw new DomainException("Unidade de medida é obrigatória.");
        NumeroItem = numeroItem;
        DescricaoItem = descricaoItem;
        Quantidade = quantidade;
        ValorUnitario = valorUnitario;
        ValorDesconto = valorDesconto;
        ValorTotal = quantidade * valorUnitario - valorDesconto;
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
            throw new DomainException("Valor do desconto não pode ser negativo.");

        if (valorDesconto > Quantidade * ValorUnitario)
            throw new DomainException("Desconto não pode ser maior que o valor total do item.");

        ValorDesconto = valorDesconto;
        AtualizarValorTotal();
    }

    public void AtualizarSku(Skus sku)
    {
        Sku = sku ?? throw new DomainException("SKU é obrigatório.");
    }

    public void AtualizarUnidadeMedida(UnidadesMedida unidadeMedida)
    {
        UnidadeMedida = unidadeMedida ?? throw new DomainException("Unidade de medida é obrigatória.");
    }

    private void AtualizarValorTotal()
    {
        ValorTotal = Quantidade * ValorUnitario - ValorDesconto;
    }
}
