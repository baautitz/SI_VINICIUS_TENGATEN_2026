using Backend.Core.Common.Exceptions;
using Backend.Core.Common.Helpers;
using Backend.Core.Features.Financeiro.Entities.Enums;
using Backend.Core.Features.Pagamentos.Entities;
using Backend.Core.Features.Parceiros.Entities;

namespace Backend.Core.Features.Financeiro.Entities;

public class ContasReceber
{
    private readonly List<ContasReceberParcelas> _parcelas = new();

    public int Id { get; private set; }
    public string Descricao { get; private set; }
    public DateTime? DataEmissao { get; private set; }
    public DateTime? DataVencimento { get; private set; }
    public decimal ValorOriginal { get; private set; }
    public decimal ValorSaldo { get; private set; }
    public StatusTituloFinanceiro Status { get; private set; }
    public string? Observacao { get; private set; }
    public DateTime CriadoEm { get; private set; }

    public Clientes Cliente { get; private set; }
    public int? NfeId { get; private set; }
    public CondicoesPagamentos? CondicaoPagamento { get; private set; }
    public int? VendaId { get; private set; }

    public IReadOnlyCollection<ContasReceberParcelas> ContasReceberParcelas => _parcelas.AsReadOnly();

    public ContasReceber(string descricao, decimal valorOriginal, Clientes cliente, DateTime? dataEmissao = null, DateTime? dataVencimento = null, CondicoesPagamentos? condicaoPagamento = null, int? nfeId = null, int? vendaId = null, string? observacao = null)
    {
        descricao = TextNormalization.Normalize(descricao);
        observacao = TextNormalization.NormalizeOrNull(observacao);

        if (string.IsNullOrWhiteSpace(descricao))
            throw new DomainException("Descrição da conta a receber é obrigatória.");

        if (valorOriginal <= 0)
            throw new DomainException("Valor original deve ser maior que zero.");

        Cliente = cliente ?? throw new DomainException("Cliente é obrigatório para contas a receber.");

        Descricao = descricao;
        ValorOriginal = valorOriginal;
        ValorSaldo = valorOriginal;
        Cliente = cliente;
        DataEmissao = dataEmissao;
        DataVencimento = dataVencimento;
        CondicaoPagamento = condicaoPagamento;
        NfeId = nfeId;
        VendaId = vendaId;
        Observacao = observacao;
        CriadoEm = DateTime.UtcNow;
        Status = StatusTituloFinanceiro.ABERTO;
    }

    public ContasReceber(int id, string descricao, decimal valorOriginal, Clientes cliente, DateTime? dataEmissao = null, DateTime? dataVencimento = null, CondicoesPagamentos? condicaoPagamento = null, int? nfeId = null, int? vendaId = null, string? observacao = null, DateTime? criadoEm = null, StatusTituloFinanceiro status = StatusTituloFinanceiro.ABERTO)
        : this(descricao, valorOriginal, cliente, dataEmissao, dataVencimento, condicaoPagamento, nfeId, vendaId, observacao)
    {
        Id = id;
        CriadoEm = criadoEm ?? DateTime.UtcNow;
        Status = status;
    }

    public void AdicionarParcelaExistente(ContasReceberParcelas parcela)
    {
        if (parcela == null)
            throw new DomainException("Parcela é obrigatória.");

        _parcelas.Add(parcela);
        AtualizarSaldo();
    }

    public void AdicionarParcela(int numeroParcela, DateTime dataVencimento, decimal valorParcela)
    {
        if (_parcelas.Any(p => p.NumeroParcela == numeroParcela))
            throw new DomainException("Já existe parcela com esse número.");

        var parcela = new ContasReceberParcelas(numeroParcela, dataVencimento, valorParcela);
        _parcelas.Add(parcela);
        AtualizarSaldo();
    }

    public void RegistrarRecebimento(int numeroParcela, decimal valorRecebido)
    {
        var parcela = _parcelas.SingleOrDefault(p => p.NumeroParcela == numeroParcela)
            ?? throw new DomainException("Parcela não encontrada.");

        parcela.RegistrarRecebimento(valorRecebido);
        AtualizarSaldo();
    }

    public void Atualizar(string descricao, decimal valorOriginal, Clientes cliente, IEnumerable<ContasReceberParcelas> parcelas, DateTime? dataEmissao = null, DateTime? dataVencimento = null, CondicoesPagamentos? condicaoPagamento = null, int? nfeId = null, int? vendaId = null, string? observacao = null)
    {
        descricao = TextNormalization.Normalize(descricao);
        observacao = TextNormalization.NormalizeOrNull(observacao);

        if (string.IsNullOrWhiteSpace(descricao))
            throw new DomainException("Descrição da conta a receber é obrigatória.");

        if (valorOriginal <= 0)
            throw new DomainException("Valor original deve ser maior que zero.");

        Cliente = cliente ?? throw new DomainException("Cliente é obrigatório para contas a receber.");

        if (Status == StatusTituloFinanceiro.PAGO || Status == StatusTituloFinanceiro.CANCELADO)
            throw new DomainException("Não é possível alterar uma conta a receber que já está paga ou cancelada.");

        if (parcelas == null || !parcelas.Any())
            throw new DomainException("A conta a receber deve conter ao menos uma parcela.");

        var totalParcelas = parcelas.Sum(p => p.ValorParcela);
        if (totalParcelas != valorOriginal)
            throw new DomainException("A soma das parcelas deve ser exatamente igual ao valor original da conta.");

        Descricao = descricao;
        ValorOriginal = valorOriginal;
        DataEmissao = dataEmissao;
        DataVencimento = dataVencimento;
        CondicaoPagamento = condicaoPagamento;
        NfeId = nfeId;
        VendaId = vendaId;
        Observacao = observacao;

        _parcelas.Clear();
        foreach (var p in parcelas)
        {
            _parcelas.Add(p);
        }

        AtualizarSaldo();
    }

    private void AtualizarSaldo()
    {
        var recebido = _parcelas.Sum(p => p.ValorRecebido);
        ValorSaldo = Math.Max(0, ValorOriginal - recebido);
        Status = ValorSaldo == 0 ? StatusTituloFinanceiro.PAGO : StatusTituloFinanceiro.PARCIAL;

        if (_parcelas.All(p => p.Status == StatusTituloFinanceiro.CANCELADO))
        {
            Status = StatusTituloFinanceiro.CANCELADO;
        }
    }
}
