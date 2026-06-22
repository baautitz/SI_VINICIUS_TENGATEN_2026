using Backend.Core.Common.Exceptions;
using Backend.Core.Common.Helpers;
using Backend.Core.Features.Financeiro.Entities.Enums;
using Backend.Core.Features.Parceiros.Entities;
using Backend.Core.Features.Financeiro.Entities;

namespace Backend.Core.Features.Financeiro.Entities;

public class ContasPagar
{
    private readonly List<ContasPagarParcelas> _parcelas = new();

    public int Id { get; private set; }
    public string Descricao { get; private set; }

    protected ContasPagar()
    {
        Descricao = string.Empty;
        Fornecedor = null!;
    }
    public DateTime? DataEmissao { get; private set; }
    public DateTime? DataVencimento { get; private set; }
    public decimal ValorOriginal { get; private set; }
    public decimal ValorSaldo { get; private set; }
    public StatusTituloFinanceiro Status { get; private set; }
    public string? Observacao { get; private set; }
    public DateTime CriadoEm { get; private set; }

    public Fornecedores Fornecedor { get; private set; }
    public int? NfeId { get; private set; }
    public CondicoesPagamentos? CondicaoPagamento { get; private set; }

    public IReadOnlyCollection<ContasPagarParcelas> ContasPagarParcelas => _parcelas.AsReadOnly();

    public ContasPagar(string descricao, decimal valorOriginal, Fornecedores fornecedor, DateTime? dataEmissao = null, DateTime? dataVencimento = null, CondicoesPagamentos? condicaoPagamento = null, int? nfeId = null, string? observacao = null)
    {
        descricao = TextNormalization.Normalize(descricao);
        observacao = TextNormalization.NormalizeOrNull(observacao);

        if (string.IsNullOrWhiteSpace(descricao))
            throw new DomainException("Descrição da conta a pagar é obrigatória.");

        if (valorOriginal <= 0)
            throw new DomainException("Valor original deve ser maior que zero.");

        Fornecedor = fornecedor ?? throw new DomainException("Fornecedor é obrigatório para contas a pagar.");

        Descricao = descricao;
        ValorOriginal = valorOriginal;
        ValorSaldo = valorOriginal;
        Fornecedor = fornecedor;
        DataEmissao = dataEmissao;
        DataVencimento = dataVencimento;
        CondicaoPagamento = condicaoPagamento;
        NfeId = nfeId;
        Observacao = observacao;
        CriadoEm = DateTime.UtcNow;
        Status = StatusTituloFinanceiro.ABERTO;
    }

    public ContasPagar(int id, string descricao, decimal valorOriginal, Fornecedores fornecedor, DateTime? dataEmissao = null, DateTime? dataVencimento = null, CondicoesPagamentos? condicaoPagamento = null, int? nfeId = null, string? observacao = null, DateTime? criadoEm = null, StatusTituloFinanceiro status = StatusTituloFinanceiro.ABERTO)
        : this(descricao, valorOriginal, fornecedor, dataEmissao, dataVencimento, condicaoPagamento, nfeId, observacao)
    {
        Id = id;
        CriadoEm = criadoEm ?? DateTime.UtcNow;
        Status = status;
    }

    public void AdicionarParcelaExistente(ContasPagarParcelas parcela)
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

        var parcela = new ContasPagarParcelas(numeroParcela, dataVencimento, valorParcela);
        _parcelas.Add(parcela);
        AtualizarSaldo();
    }

    public void RegistrarPagamento(int numeroParcela, decimal valorPago)
    {
        var parcela = _parcelas.SingleOrDefault(p => p.NumeroParcela == numeroParcela)
            ?? throw new DomainException("Parcela não encontrada.");

        parcela.RegistrarPagamento(valorPago);
        AtualizarSaldo();
    }

    public void EstornarPagamento(int numeroParcela, decimal valorEstornado)
    {
        var parcela = _parcelas.SingleOrDefault(p => p.NumeroParcela == numeroParcela)
            ?? throw new DomainException("Parcela não encontrada.");

        parcela.EstornarPagamento(valorEstornado);
        AtualizarSaldo();
    }

    public void Atualizar(string descricao, decimal valorOriginal, Fornecedores fornecedor, IEnumerable<ContasPagarParcelas> parcelas, DateTime? dataEmissao = null, DateTime? dataVencimento = null, CondicoesPagamentos? condicaoPagamento = null, int? nfeId = null, string? observacao = null)
    {
        descricao = TextNormalization.Normalize(descricao);
        observacao = TextNormalization.NormalizeOrNull(observacao);

        if (string.IsNullOrWhiteSpace(descricao))
            throw new DomainException("Descrição da conta a pagar é obrigatória.");

        if (valorOriginal <= 0)
            throw new DomainException("Valor original deve ser maior que zero.");

        Fornecedor = fornecedor ?? throw new DomainException("Fornecedor é obrigatório para contas a pagar.");

        if (Status == StatusTituloFinanceiro.PAGO || Status == StatusTituloFinanceiro.CANCELADO)
            throw new DomainException("Não é possível alterar uma conta a pagar que já está paga ou cancelada.");

        var temParcelaBaixada = _parcelas.Any(p => p.Status == StatusTituloFinanceiro.PAGO || p.Status == StatusTituloFinanceiro.PARCIAL || p.ValorPago > 0);
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
                if (nova.ValorPago != existente.ValorPago)
                    throw new DomainException($"Não é possível alterar o valor pago da parcela {nova.NumeroParcela} diretamente.");
                if (nova.Status != existente.Status)
                    throw new DomainException($"Não é possível alterar o status da parcela {nova.NumeroParcela} diretamente.");
            }
        }

        if (parcelas == null || !parcelas.Any())
            throw new DomainException("A conta a pagar deve conter ao menos uma parcela.");

        var totalParcelas = parcelas.Sum(p => p.ValorParcela);
        if (totalParcelas != valorOriginal)
            throw new DomainException("A soma das parcelas deve ser exactly igual ao valor original da conta.");

        Descricao = descricao;
        ValorOriginal = valorOriginal;
        DataEmissao = dataEmissao;
        DataVencimento = dataVencimento;
        CondicaoPagamento = condicaoPagamento;
        NfeId = nfeId;
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
        var pago = _parcelas.Sum(p => p.ValorPago);
        ValorSaldo = Math.Max(0, ValorOriginal - pago);

        if (ValorSaldo == 0)
            Status = StatusTituloFinanceiro.PAGO;
        else if (pago > 0)
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

