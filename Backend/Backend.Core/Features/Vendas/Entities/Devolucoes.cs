using Backend.Core.Common.Exceptions;
using Backend.Core.Common.Helpers;

namespace Backend.Core.Features.Vendas.Entities;

// Imutável: depois de registrada nunca é alterada nem excluída.
public class Devolucao
{
    private readonly List<DevolucaoItens> _itens = new();

    public int Id { get; set; }
    public int VendaId { get; private set; }
    public DateTime DataDevolucao { get; private set; }
    public string Motivo { get; private set; } = null!;
    public decimal ValorTotal { get; private set; }
    public IReadOnlyCollection<DevolucaoItens> Itens => _itens.AsReadOnly();

    protected Devolucao() { }

    public Devolucao(Venda venda, string motivo, IEnumerable<DevolucaoItens> itens)
    {
        if (venda.DataCancelamento != null)
            throw new DomainException("Não é possível devolver itens de uma venda cancelada.");

        motivo = TextNormalization.Normalize(motivo);
        if (motivo.Length < 5)
            throw new DomainException("Motivo da devolução é obrigatório e deve ter pelo menos 5 caracteres.");

        _itens.AddRange(itens);
        if (_itens.Count == 0)
            throw new DomainException("Informe ao menos um item com quantidade maior que zero.");

        VendaId = venda.Id;
        Motivo = motivo;
        DataDevolucao = DateTime.UtcNow;
        ValorTotal = Math.Round(_itens.Sum(i => i.ValorTotal), 2);
    }

    public Devolucao(int id, int vendaId, DateTime dataDevolucao, string motivo, decimal valorTotal)
    {
        Id = id;
        VendaId = vendaId;
        DataDevolucao = dataDevolucao;
        Motivo = motivo;
        ValorTotal = valorTotal;
    }

    public void AdicionarItemExistente(DevolucaoItens item) => _itens.Add(item);
}

public class DevolucaoItens
{
    public int Id { get; private set; }
    public int VendaItemId { get; private set; }
    public string Sku { get; private set; } = null!;
    public decimal Quantidade { get; private set; }
    public decimal ValorUnitario { get; private set; }
    public decimal CustoUnitario { get; private set; }
    public decimal ValorTotal => Quantidade * ValorUnitario;

    protected DevolucaoItens() { }

    // Valor unitário devolvido = valor líquido do item (já com o desconto rateado); custo = custo da saída original.
    public DevolucaoItens(VendaItens itemVenda, decimal quantidade, decimal custoUnitario)
    {
        if (quantidade <= 0)
            throw new DomainException("Quantidade devolvida deve ser maior que zero.");

        if (quantidade > itemVenda.SaldoDevolvivel)
            throw new DomainException($"Quantidade devolvida do SKU '{itemVenda.Sku.Sku}' excede o saldo devolvível ({itemVenda.SaldoDevolvivel:0.####}).");

        VendaItemId = itemVenda.Id;
        Sku = itemVenda.Sku.Sku;
        Quantidade = quantidade;
        ValorUnitario = Math.Round(itemVenda.ValorTotal / itemVenda.Quantidade, 4);
        CustoUnitario = custoUnitario;
    }

    public DevolucaoItens(int id, int vendaItemId, string sku, decimal quantidade, decimal valorUnitario, decimal custoUnitario)
    {
        Id = id;
        VendaItemId = vendaItemId;
        Sku = sku;
        Quantidade = quantidade;
        ValorUnitario = valorUnitario;
        CustoUnitario = custoUnitario;
    }
}
