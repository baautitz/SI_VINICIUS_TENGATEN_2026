using Backend.Core.Common.Exceptions;
using Backend.Core.Common.Helpers;
using Backend.Core.Features.Compras.Services;
using Backend.Core.Features.Parceiros.Entities;

namespace Backend.Core.Features.Compras.Entities;

// Compra nasce já recebida: sem status; o desfazer é o estorno (registro permanece, marcado como estornado).
public class Compra
{
    private readonly List<CompraItens> _itens = new();

    public int Id { get; set; }
    public DateTime DataCompra { get; private set; }
    public decimal ValorNota { get; private set; }
    public decimal ValorFrete { get; private set; }
    public decimal ValorSeguro { get; private set; }
    public decimal ValorOutrasDespesas { get; private set; }
    public decimal ValorDesconto { get; private set; }
    public decimal ValorFreteTerceiro { get; private set; }
    public int? TransportadoraId { get; private set; }
    public int? CondicaoPagamentoId { get; private set; }
    // Custo total da compra: nota + frete de terceiro.
    public decimal ValorTotal { get; private set; }
    public string? Observacao { get; private set; }
    public DateTime? EstornadaEm { get; private set; }
    public string? MotivoEstorno { get; private set; }
    public int? UsuarioEstornoId { get; private set; }
    public bool Estornada => EstornadaEm.HasValue;
    // NF-e de entrada ligada à compra (importação de XML), se houver.
    public int? NfeId { get; private set; }

    public Emitentes Emitente { get; private set; } = null!;
    public Fornecedores Fornecedor { get; private set; } = null!;
    public IReadOnlyCollection<CompraItens> Itens => _itens.AsReadOnly();

    protected Compra() { }

    public Compra(
        DateTime dataCompra, Emitentes emitente, Fornecedores fornecedor, IEnumerable<CompraItens> itens,
        decimal valorFrete, decimal valorSeguro, decimal valorOutrasDespesas, decimal valorDesconto,
        decimal valorFreteTerceiro, int? transportadoraId, int? condicaoPagamentoId, string? observacao = null)
    {
        if (dataCompra == default)
            throw new DomainException("Data da compra é obrigatória.");
        if (new[] { valorFrete, valorSeguro, valorOutrasDespesas, valorDesconto, valorFreteTerceiro }.Any(v => v < 0))
            throw new DomainException("Despesas e desconto não podem ser negativos.");
        if (valorFreteTerceiro > 0 && transportadoraId is null)
            throw new DomainException("Frete de terceiro exige a transportadora.");

        Emitente = emitente ?? throw new DomainException("Emitente é obrigatório.");
        Fornecedor = fornecedor ?? throw new DomainException("Fornecedor é obrigatório.");
        _itens.AddRange(itens ?? Array.Empty<CompraItens>());
        if (_itens.Count == 0)
            throw new DomainException("A compra deve conter ao menos um item.");

        DataCompra = dataCompra;
        ValorFrete = valorFrete;
        ValorSeguro = valorSeguro;
        ValorOutrasDespesas = valorOutrasDespesas;
        ValorDesconto = valorDesconto;
        ValorFreteTerceiro = valorFreteTerceiro;
        TransportadoraId = transportadoraId;
        CondicaoPagamentoId = condicaoPagamentoId;
        Observacao = TextNormalization.NormalizeOrNull(observacao);

        var produtos = _itens.Sum(i => i.ValorTotal);
        ValorNota = Math.Round(produtos + valorFrete + valorSeguro + valorOutrasDespesas - valorDesconto, 2, MidpointRounding.AwayFromZero);
        ValorTotal = Math.Round(ValorNota + valorFreteTerceiro, 2, MidpointRounding.AwayFromZero);

        var rateios = RateioCompra.Ratear(_itens.Select(i => i.ValorTotal).ToList(),
            valorFrete + valorSeguro + valorOutrasDespesas + valorFreteTerceiro - valorDesconto);
        for (var i = 0; i < _itens.Count; i++)
            _itens[i].DefinirRateio(rateios[i]);
    }

    public void Estornar(string? motivo, int? usuarioId, DateTime? quando = null)
    {
        if (Estornada)
            throw new DomainException("A compra já foi estornada.");

        motivo = (motivo ?? "").Trim(); // texto livre: preserva caixa e acentos
        if (motivo.Length < 5 || motivo.Length > 255)
            throw new DomainException("O motivo do estorno deve ter entre 5 e 255 caracteres.");

        MotivoEstorno = motivo;
        UsuarioEstornoId = usuarioId;
        EstornadaEm = quando ?? DateTime.UtcNow;
    }

    public void AtualizarEmitente(Emitentes emitente) => Emitente = emitente;
    public void AtualizarFornecedor(Fornecedores fornecedor) => Fornecedor = fornecedor;

    public void DefinirItens(IEnumerable<CompraItens> itens)
    {
        _itens.Clear();
        _itens.AddRange(itens);
    }
}
