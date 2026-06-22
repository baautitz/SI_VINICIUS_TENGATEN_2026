using Backend.Core.Common.Exceptions;
using Backend.Core.Common.Helpers;
using Backend.Core.Features.Financeiro.Entities.Enums;
using Backend.Core.Features.Financeiro.Entities;
using Backend.Core.Features.Parceiros.Entities;

namespace Backend.Core.Features.Financeiro.Entities;

public class ContasReceber
{
    private readonly List<ContasReceberParcelas> _parcelas = new();

    public int Id { get; private set; }
    public string Descricao { get; private set; }

    protected ContasReceber()
    {
        Descricao = string.Empty;
        Cliente = null!;
    }
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

    public void EstornarRecebimento(int numeroParcela, decimal valorEstornado)
    {
        var parcela = _parcelas.SingleOrDefault(p => p.NumeroParcela == numeroParcela)
            ?? throw new DomainException("Parcela não encontrada.");

        parcela.EstornarRecebimento(valorEstornado);
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

        var temParcelaBaixada = _parcelas.Any(p => p.Status == StatusTituloFinanceiro.PAGO || p.Status == StatusTituloFinanceiro.PARCIAL || p.ValorRecebido > 0);
        if (temParcelaBaixada)
        {
            if (valorOriginal != ValorOriginal)
                throw new DomainException("Não é possível alterar o valor original de uma conta com parcelas baixadas.");
            if (condicaoPagamento?.Id != CondicaoPagamento?.Id)
                throw new DomainException("Não é possível alterar a condição de pagamento de uma conta com parcelas baixadas.");
            if (dataEmissao?.Date != DataEmissao?.Date)
                throw new DomainException("Não é possível alterar a data de emissão de uma conta com parcelas baixadas.");
            
            var novasParcelas = parcelas.ToList();
            if (novasParcelas.Count != _parcelas.Count)
                throw new DomainException("Não é possível alterar a quantidade de parcelas de uma conta com baixas realizadas.");

            foreach (var nova in novasParcelas)
            {
                var existente = _parcelas.SingleOrDefault(p => p.NumeroParcela == nova.NumeroParcela)
                    ?? throw new DomainException($"Parcela número {nova.NumeroParcela} não encontrada na conta original.");
                
                if (nova.ValorParcela != existente.ValorParcela)
                    throw new DomainException($"Não é possível alterar o valor da parcela {nova.NumeroParcela} pois o título possui baixas.");
                if (nova.DataVencimento.Date != existente.DataVencimento.Date)
                    throw new DomainException($"Não é possível alterar o vencimento da parcela {nova.NumeroParcela} pois o título possui baixas.");
                if (nova.ValorRecebido != existente.ValorRecebido)
                    throw new DomainException($"Não é possível alterar o valor recebido da parcela {nova.NumeroParcela} diretamente.");
                if (nova.Status != existente.Status)
                    throw new DomainException($"Não é possível alterar o status da parcela {nova.NumeroParcela} diretamente.");
            }
        }

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

        if (ValorSaldo == 0)
            Status = StatusTituloFinanceiro.PAGO;
        else if (recebido > 0)
            Status = StatusTituloFinanceiro.PARCIAL;
        else
            Status = StatusTituloFinanceiro.ABERTO;

        if (_parcelas.All(p => p.Status == StatusTituloFinanceiro.CANCELADO))
        {
            Status = StatusTituloFinanceiro.CANCELADO;
        }

        DataVencimento = _parcelas
            .Where(p => p.Status == StatusTituloFinanceiro.ABERTO || p.Status == StatusTituloFinanceiro.PARCIAL)
            .OrderBy(p => p.DataVencimento)
            .FirstOrDefault()?.DataVencimento;
    }
}

