using Backend.Core.Common.Exceptions;
using Backend.Core.Features.Compras.Entities.Enums;

namespace Backend.Core.Features.Compras.Entities;

// Devolução de compra: itens da compra devolvidos ao fornecedor. Depois de registrada não é alterada nem excluída; só pode ser estornada (uma vez).
public class DevolucaoCompra
{
    private readonly List<DevolucaoCompraItens> _itens = new();

    public int Id { get; set; }
    public int CompraId { get; private set; }
    public DateTime DataDevolucao { get; private set; }
    public string Motivo { get; private set; } = null!;
    public decimal ValorTotal { get; private set; }
    public StatusDevolucaoCompra Status { get; private set; } = StatusDevolucaoCompra.ATIVA;
    public DateTime? EstornadoEm { get; private set; }
    public string? MotivoEstorno { get; private set; }
    public IReadOnlyCollection<DevolucaoCompraItens> Itens => _itens.AsReadOnly();

    protected DevolucaoCompra() { }

    public DevolucaoCompra(Compra compra, string motivo, IEnumerable<DevolucaoCompraItens> itens)
    {
        motivo = (motivo ?? "").Trim(); // texto livre: preserva caixa e acentos
        if (motivo.Length < 5 || motivo.Length > 255)
            throw new DomainException("O motivo da devolução deve ter entre 5 e 255 caracteres.");

        _itens.AddRange(itens);
        if (_itens.Count == 0)
            throw new DomainException("Informe ao menos um item com quantidade maior que zero.");

        CompraId = compra.Id;
        Motivo = motivo;
        DataDevolucao = DateTime.UtcNow;
        ValorTotal = Math.Round(_itens.Sum(i => i.ValorTotal), 2, MidpointRounding.AwayFromZero);
    }

    public DevolucaoCompra(int id, int compraId, DateTime dataDevolucao, string motivo, decimal valorTotal, StatusDevolucaoCompra status = StatusDevolucaoCompra.ATIVA, DateTime? estornadoEm = null, string? motivoEstorno = null)
    {
        Id = id;
        CompraId = compraId;
        DataDevolucao = dataDevolucao;
        Motivo = motivo;
        ValorTotal = valorTotal;
        Status = status;
        EstornadoEm = estornadoEm;
        MotivoEstorno = motivoEstorno;
    }

    public void Estornar(string motivo, DateTime? quando = null)
    {
        if (Status == StatusDevolucaoCompra.ESTORNADA)
            throw new DomainException("A devolução já foi estornada.");

        motivo = (motivo ?? "").Trim(); // texto livre: preserva caixa e acentos
        if (motivo.Length < 5 || motivo.Length > 255)
            throw new DomainException("O motivo do estorno deve ter entre 5 e 255 caracteres.");

        Status = StatusDevolucaoCompra.ESTORNADA;
        MotivoEstorno = motivo;
        EstornadoEm = quando ?? DateTime.UtcNow;
    }

    public void AdicionarItemExistente(DevolucaoCompraItens item) => _itens.Add(item);
}

public class DevolucaoCompraItens
{
    public int Id { get; private set; }
    public int CompraItemId { get; private set; }
    public string Sku { get; private set; } = null!;
    public decimal Quantidade { get; private set; }
    public decimal ValorUnitario { get; private set; }
    public decimal CustoUnitario { get; private set; }
    public decimal ValorTotal => Quantidade * ValorUnitario;

    protected DevolucaoCompraItens() { }

    // Valor unitário = valor líquido do item na compra (com o desconto); custo = o que o item somou ao custo médio.
    public DevolucaoCompraItens(CompraItens itemCompra, decimal quantidade)
    {
        if (quantidade <= 0)
            throw new DomainException("Quantidade devolvida deve ser maior que zero.");

        CompraItemId = itemCompra.Id;
        Sku = itemCompra.Sku.Sku;
        Quantidade = quantidade;
        ValorUnitario = Math.Round(itemCompra.ValorTotal / itemCompra.Quantidade, 4);
        CustoUnitario = itemCompra.CustoUnitario;
    }

    public DevolucaoCompraItens(int id, int compraItemId, string sku, decimal quantidade, decimal valorUnitario, decimal custoUnitario)
    {
        Id = id;
        CompraItemId = compraItemId;
        Sku = sku;
        Quantidade = quantidade;
        ValorUnitario = valorUnitario;
        CustoUnitario = custoUnitario;
    }
}

// Quanto uma devolução de compra abateu de uma parcela a pagar; estornado marca o registro sem apagá-lo.
public class DevolucaoCompraAbatimento
{
    public int Id { get; private set; }
    public int DevolucaoCompraId { get; private set; }
    public int ContaPagarId { get; private set; }
    public decimal Valor { get; private set; }
    public DateTime? EstornadoEm { get; private set; }

    protected DevolucaoCompraAbatimento() { }

    public DevolucaoCompraAbatimento(int devolucaoCompraId, int contaPagarId, decimal valor)
    {
        if (valor <= 0)
            throw new DomainException("Valor do abatimento deve ser maior que zero.");

        DevolucaoCompraId = devolucaoCompraId;
        ContaPagarId = contaPagarId;
        Valor = valor;
    }

    public DevolucaoCompraAbatimento(int id, int devolucaoCompraId, int contaPagarId, decimal valor, DateTime? estornadoEm)
        : this(devolucaoCompraId, contaPagarId, valor)
    {
        Id = id;
        EstornadoEm = estornadoEm;
    }
}
