using Backend.Core.Common.Exceptions;
using Backend.Core.Common.Helpers;
using Backend.Core.Features.Vendas.Entities.Enums;

namespace Backend.Core.Features.Vendas.Entities;

// Depois de registrada nunca é alterada nem excluída; só pode ser estornada (uma vez).
public class Devolucao
{
    private readonly List<DevolucaoItens> _itens = new();

    public int Id { get; set; }
    public int VendaId { get; private set; }
    public DateTime DataDevolucao { get; private set; }
    public string Motivo { get; private set; } = null!;
    public decimal ValorTotal { get; private set; }
    public StatusDevolucao Status { get; private set; } = StatusDevolucao.ATIVA;
    public DateTime? EstornadoEm { get; private set; }
    public string? MotivoEstorno { get; private set; }
    // Aviso na resposta da criação: NF-e de saída ainda ativa da venda (não persistido).
    public Backend.Core.Features.NFe.DTOs.NfeAtiva? NfeAtiva { get; set; }
    public IReadOnlyCollection<DevolucaoItens> Itens => _itens.AsReadOnly();

    protected Devolucao() { }

    public Devolucao(Venda venda, string motivo, IEnumerable<DevolucaoItens> itens)
    {
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

    public Devolucao(int id, int vendaId, DateTime dataDevolucao, string motivo, decimal valorTotal, StatusDevolucao status = StatusDevolucao.ATIVA, DateTime? estornadoEm = null, string? motivoEstorno = null)
    {
        Id = id;
        VendaId = vendaId;
        DataDevolucao = dataDevolucao;
        Motivo = motivo;
        ValorTotal = valorTotal;
        Status = status;
        EstornadoEm = estornadoEm;
        MotivoEstorno = motivoEstorno;
    }

    public void Estornar(string motivo, DateTime? quando = null)
    {
        if (Status == StatusDevolucao.ESTORNADA)
            throw new DomainException("A devolução já foi estornada.");

        motivo = (motivo ?? "").Trim(); // texto livre: preserva caixa e acentos
        if (motivo.Length < 5 || motivo.Length > 255)
            throw new DomainException("O motivo do estorno deve ter entre 5 e 255 caracteres.");

        Status = StatusDevolucao.ESTORNADA;
        MotivoEstorno = motivo;
        EstornadoEm = quando ?? DateTime.UtcNow;
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
