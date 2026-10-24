using Backend.Core.Common.Exceptions;
using Backend.Core.Common.Helpers;
using Backend.Core.Features.Financeiro.Entities.Enums;
using Backend.Core.Features.Parceiros.Entities;

namespace Backend.Core.Features.Financeiro.Entities;

// Uma linha = uma parcela; o documento de origem (origem_tipo + origem_id / numero_documento) agrupa as parcelas.
public class ContasPagar
{
    public int Id { get; private set; }
    public OrigemTituloFinanceiro OrigemTipo { get; private set; }
    public int? OrigemId { get; private set; }
    public string NumeroDocumento { get; private set; }
    public int NumeroParcela { get; private set; }
    public int TotalParcelas { get; private set; }
    public string Descricao { get; private set; }

    // Contraparte: exatamente um entre fornecedor, cliente (devolução) e transportadora (frete).
    public Fornecedores? Fornecedor { get; private set; }
    public int? ClienteId { get; private set; }
    public string? ClienteNome { get; private set; }
    public int? TransportadoraId { get; private set; }
    public string? TransportadoraNome { get; private set; }

    public DateTime? DataEmissao { get; private set; }
    public DateTime DataVencimento { get; private set; }
    public decimal Valor { get; private set; }
    public decimal ValorPago { get; private set; }
    public decimal ValorSaldo => Valor - ValorPago;
    public StatusTituloFinanceiro Status { get; private set; }
    public CondicoesPagamentos? CondicaoPagamento { get; private set; }
    public string? Observacao { get; private set; }
    public DateTime CriadoEm { get; private set; }

    protected ContasPagar()
    {
        Descricao = string.Empty;
        NumeroDocumento = string.Empty;
    }

    public ContasPagar(string descricao, decimal valor, Fornecedores? fornecedor, DateTime dataVencimento, string numeroDocumento, int numeroParcela = 1, int totalParcelas = 1, DateTime? dataEmissao = null, CondicoesPagamentos? condicaoPagamento = null, OrigemTituloFinanceiro origemTipo = OrigemTituloFinanceiro.MANUAL, int? origemId = null, string? observacao = null, int? clienteId = null, string? clienteNome = null, int? transportadoraId = null, string? transportadoraNome = null)
    {
        descricao = TextNormalization.Normalize(descricao);
        observacao = TextNormalization.NormalizeOrNull(observacao);
        numeroDocumento = TextNormalization.Normalize(numeroDocumento);

        if (string.IsNullOrWhiteSpace(descricao))
            throw new DomainException("Descrição da conta a pagar é obrigatória.");

        if (string.IsNullOrWhiteSpace(numeroDocumento) || numeroDocumento.Length > 30)
            throw new DomainException("Número do documento é obrigatório e deve ter no máximo 30 caracteres.");

        if (valor <= 0)
            throw new DomainException("Valor deve ser maior que zero.");

        if (dataVencimento == default)
            throw new DomainException("Data de vencimento é obrigatória.");

        if (numeroParcela <= 0 || totalParcelas < numeroParcela)
            throw new DomainException("Número da parcela inválido.");

        if (origemTipo == OrigemTituloFinanceiro.VENDA)
            throw new DomainException("Conta a pagar não pode ter origem em venda.");

        if (origemTipo != OrigemTituloFinanceiro.MANUAL && origemId is null)
            throw new DomainException("O documento de origem é obrigatório.");

        if ((fornecedor is null ? 0 : 1) + (clienteId is null ? 0 : 1) + (transportadoraId is null ? 0 : 1) != 1)
            throw new DomainException("A conta a pagar deve ter exatamente um fornecedor, cliente ou transportadora.");

        if (origemTipo == OrigemTituloFinanceiro.DEVOLUCAO_VENDA && clienteId is null)
            throw new DomainException("Reembolso de devolução exige cliente.");

        if (origemTipo == OrigemTituloFinanceiro.FRETE_COMPRA && transportadoraId is null)
            throw new DomainException("Frete da compra exige transportadora.");

        if (origemTipo is OrigemTituloFinanceiro.MANUAL or OrigemTituloFinanceiro.COMPRA && fornecedor is null)
            throw new DomainException("Fornecedor é obrigatório para contas a pagar.");

        Fornecedor = fornecedor;
        ClienteId = clienteId;
        ClienteNome = clienteNome;
        TransportadoraId = transportadoraId;
        TransportadoraNome = transportadoraNome;
        Descricao = descricao;
        NumeroDocumento = numeroDocumento;
        NumeroParcela = numeroParcela;
        TotalParcelas = totalParcelas;
        Valor = Math.Round(valor, 2, MidpointRounding.AwayFromZero);
        ValorPago = 0;
        DataEmissao = dataEmissao;
        DataVencimento = dataVencimento;
        CondicaoPagamento = condicaoPagamento;
        OrigemTipo = origemTipo;
        OrigemId = origemId;
        Observacao = observacao;
        CriadoEm = DateTime.UtcNow;
        Status = StatusTituloFinanceiro.ABERTO;
    }

    // Reidratação a partir do banco: confia nas regras do schema (uma contraparte entre fornecedor, cliente e transportadora).
    public ContasPagar(int id, string descricao, decimal valor, Fornecedores? fornecedor, DateTime dataVencimento, string numeroDocumento, int numeroParcela, int totalParcelas, DateTime? dataEmissao, CondicoesPagamentos? condicaoPagamento, OrigemTituloFinanceiro origemTipo, int? origemId, string? observacao, DateTime criadoEm, decimal valorPago, StatusTituloFinanceiro status, int? clienteId, string? clienteNome, int? transportadoraId, string? transportadoraNome = null)
    {
        Id = id;
        Fornecedor = fornecedor;
        ClienteId = clienteId;
        ClienteNome = clienteNome;
        TransportadoraId = transportadoraId;
        TransportadoraNome = transportadoraNome;
        Descricao = descricao;
        NumeroDocumento = numeroDocumento;
        NumeroParcela = numeroParcela;
        TotalParcelas = totalParcelas;
        Valor = valor;
        ValorPago = valorPago;
        DataEmissao = dataEmissao;
        DataVencimento = dataVencimento;
        CondicaoPagamento = condicaoPagamento;
        OrigemTipo = origemTipo;
        OrigemId = origemId;
        Observacao = observacao;
        CriadoEm = criadoEm;
        Status = status;
    }

    public void RegistrarPagamento(decimal valor)
    {
        valor = Math.Round(valor, 2, MidpointRounding.AwayFromZero);

        if (valor <= 0)
            throw new DomainException("Valor de pagamento deve ser maior que zero.");

        if (Status == StatusTituloFinanceiro.PAGO)
            throw new DomainException("Não é possível pagar uma parcela que já está quitada.");

        if (Status == StatusTituloFinanceiro.CANCELADO)
            throw new DomainException("Não é possível pagar uma parcela cancelada.");

        if (valor > ValorSaldo)
            throw new DomainException($"O valor informado (R$ {valor:F2}) excede o saldo restante da parcela (R$ {ValorSaldo:F2}).");

        ValorPago += valor;
        Status = ValorPago >= Valor ? StatusTituloFinanceiro.PAGO : StatusTituloFinanceiro.PARCIAL;
    }

    public void EstornarPagamento(decimal valor)
    {
        valor = Math.Round(valor, 2, MidpointRounding.AwayFromZero);

        if (valor <= 0)
            throw new DomainException("Valor de estorno deve ser maior que zero.");

        if (Status == StatusTituloFinanceiro.CANCELADO)
            throw new DomainException("Não é possível estornar uma parcela cancelada.");

        if (valor > ValorPago)
            throw new DomainException($"O valor do estorno (R$ {valor:F2}) não pode ser maior do que o valor já pago (R$ {ValorPago:F2}).");

        ValorPago -= valor;
        Status = ValorPago == 0 ? StatusTituloFinanceiro.ABERTO : StatusTituloFinanceiro.PARCIAL;
    }

    // Cancela a parcela (ex.: estorno da compra de origem); exige que nada tenha sido pago.
    public void Cancelar()
    {
        if (ValorPago > 0)
            throw new DomainException("Não é possível cancelar uma conta a pagar com parcelas baixadas.");

        Status = StatusTituloFinanceiro.CANCELADO;
    }

    public void Atualizar(string descricao, DateTime dataVencimento, decimal valor, string? observacao)
    {
        descricao = TextNormalization.Normalize(descricao);

        if (OrigemTipo != OrigemTituloFinanceiro.MANUAL)
            throw new DomainException("Conta gerada por outro documento não pode ser editada manualmente.");

        if (Status != StatusTituloFinanceiro.ABERTO || ValorPago > 0)
            throw new DomainException("Só é possível alterar uma parcela aberta e sem pagamentos.");

        if (string.IsNullOrWhiteSpace(descricao))
            throw new DomainException("Descrição da conta a pagar é obrigatória.");

        if (valor <= 0)
            throw new DomainException("Valor deve ser maior que zero.");

        if (dataVencimento == default)
            throw new DomainException("Data de vencimento é obrigatória.");

        Descricao = descricao;
        DataVencimento = dataVencimento;
        Valor = Math.Round(valor, 2, MidpointRounding.AwayFromZero);
        Observacao = TextNormalization.NormalizeOrNull(observacao);
    }
}
