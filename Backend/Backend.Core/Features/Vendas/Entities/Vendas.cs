using Backend.Core.Common;
using Backend.Core.Features.Parceiros.Entities;

namespace Backend.Core.Features.Vendas.Entities;

public class Venda
{
    private readonly List<VendaItens> _itens = new();

    public int Id { get; set; }
    public DateTime DataVenda { get; private set; }
    public decimal ValorTotal { get; private set; }
    public string? Observacao { get; private set; }

    public Emitentes Emitente { get; private set; }
    public Clientes Cliente { get; private set; }
    public IReadOnlyCollection<VendaItens> Itens => _itens.AsReadOnly();

    public Venda(DateTime dataVenda, Emitentes emitente, Clientes cliente, IEnumerable<VendaItens> itens, string? observacao = null)
    {
        if (dataVenda == default)
            throw new DomainException("Data da venda é obrigatória.");

        Emitente = emitente ?? throw new DomainException("Emitente é obrigatório.");
        Cliente = cliente ?? throw new DomainException("Cliente é obrigatório.");

        if (itens == null || !itens.Any())
            throw new DomainException("A venda deve conter ao menos um item.");

        DataVenda = dataVenda;
        Observacao = TextNormalization.NormalizeOrNull(observacao);
        _itens.AddRange(itens);
        AtualizarTotal();
    }

    public void AdicionarItem(VendaItens item)
    {
        if (item == null)
            throw new DomainException("Item é obrigatório.");

        _itens.Add(item);
        AtualizarTotal();
    }

    public void RemoverItem(VendaItens item)
    {
        if (item == null)
            throw new DomainException("Item é obrigatório.");

        _itens.Remove(item);
        if (!_itens.Any())
            throw new DomainException("A venda deve conter ao menos um item.");

        AtualizarTotal();
    }

    public void AtualizarObservacao(string? observacao)
    {
        Observacao = TextNormalization.NormalizeOrNull(observacao);
    }

    public void AtualizarEmitente(Emitentes emitente)
    {
        Emitente = emitente ?? throw new DomainException("Emitente é obrigatório.");
    }

    public void AtualizarCliente(Clientes cliente)
    {
        Cliente = cliente ?? throw new DomainException("Cliente é obrigatório.");
    }

    public void DefinirItens(IEnumerable<VendaItens> itens)
    {
        if (itens == null)
            throw new DomainException("Itens da venda são obrigatórios.");

        _itens.Clear();
        _itens.AddRange(itens);
        AtualizarTotal();
    }

    public void AtualizarDataVenda(DateTime dataVenda)
    {
        if (dataVenda == default)
            throw new DomainException("Data da venda é obrigatória.");

        DataVenda = dataVenda;
    }

    private void AtualizarTotal()
    {
        ValorTotal = _itens.Sum(item => item.ValorTotal);

        if (ValorTotal < 0)
            throw new DomainException("Valor total da venda não pode ser negativo.");
    }
}
