using Backend.Core.Common.Exceptions;
using Backend.Core.Common.Helpers;
using Backend.Core.Features.Financeiro.Entities.Enums;
using Backend.Core.Features.Parceiros.Entities;

namespace Backend.Core.Features.Financeiro.Entities;

// Uma linha = uma parcela; o documento de origem (origem_tipo + origem_id / numero_documento) agrupa as parcelas.
public class ContasReceber
{
    public int Id { get; private set; }
    public OrigemTituloFinanceiro OrigemTipo { get; private set; }
    public int? OrigemId { get; private set; }
    public string NumeroDocumento { get; private set; }
    public int NumeroParcela { get; private set; }
    public int TotalParcelas { get; private set; }
    public string Descricao { get; private set; }
    public Clientes Cliente { get; private set; }
    public DateTime? DataEmissao { get; private set; }
    public DateTime DataVencimento { get; private set; }
    public decimal Valor { get; private set; }
    public decimal ValorPago { get; private set; }
    // Baixa não monetária por devolução: abate o saldo sem alterar o Valor original nem o que foi recebido.
    public decimal ValorAbatido { get; private set; }
    public decimal ValorSaldo => Valor - ValorPago - ValorAbatido;
    // Baixa = liquidação em dinheiro (ValorPago) ou por devolução (ValorAbatido); trava cancelamento, edição e exclusão.
    public bool PossuiBaixa => ValorPago > 0 || ValorAbatido > 0;
    public StatusTituloFinanceiro Status { get; private set; }
    public CondicoesPagamentos? CondicaoPagamento { get; private set; }
    public string? Observacao { get; private set; }
    public DateTime CriadoEm { get; private set; }

    protected ContasReceber()
    {
        Descricao = string.Empty;
        NumeroDocumento = string.Empty;
        Cliente = null!;
    }

    public ContasReceber(string descricao, decimal valor, Clientes cliente, DateTime dataVencimento, string numeroDocumento, int numeroParcela = 1, int totalParcelas = 1, DateTime? dataEmissao = null, CondicoesPagamentos? condicaoPagamento = null, OrigemTituloFinanceiro origemTipo = OrigemTituloFinanceiro.MANUAL, int? origemId = null, string? observacao = null)
    {
        descricao = TextNormalization.Normalize(descricao);
        observacao = TextNormalization.NormalizeOrNull(observacao);
        numeroDocumento = TextNormalization.Normalize(numeroDocumento);

        if (string.IsNullOrWhiteSpace(descricao))
            throw new DomainException("Descrição da conta a receber é obrigatória.");

        if (string.IsNullOrWhiteSpace(numeroDocumento) || numeroDocumento.Length > 30)
            throw new DomainException("Número do documento é obrigatório e deve ter no máximo 30 caracteres.");

        if (valor <= 0)
            throw new DomainException("Valor deve ser maior que zero.");

        if (dataVencimento == default)
            throw new DomainException("Data de vencimento é obrigatória.");

        if (numeroParcela <= 0 || totalParcelas < numeroParcela)
            throw new DomainException("Número da parcela inválido.");

        if (origemTipo is OrigemTituloFinanceiro.COMPRA or OrigemTituloFinanceiro.DEVOLUCAO_VENDA or OrigemTituloFinanceiro.FRETE_COMPRA)
            throw new DomainException("Conta a receber não pode ter origem em compra ou devolução.");

        if (origemTipo != OrigemTituloFinanceiro.MANUAL && origemId is null)
            throw new DomainException("O documento de origem é obrigatório.");

        Cliente = cliente ?? throw new DomainException("Cliente é obrigatório para contas a receber.");
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

    // Reidratação a partir do banco.
    public ContasReceber(int id, string descricao, decimal valor, Clientes cliente, DateTime dataVencimento, string numeroDocumento, int numeroParcela, int totalParcelas, DateTime? dataEmissao, CondicoesPagamentos? condicaoPagamento, OrigemTituloFinanceiro origemTipo, int? origemId, string? observacao, DateTime criadoEm, decimal valorPago, StatusTituloFinanceiro status, decimal valorAbatido = 0)
        : this(descricao, valor, cliente, dataVencimento, numeroDocumento, numeroParcela, totalParcelas, dataEmissao, condicaoPagamento, origemTipo, origemId, observacao)
    {
        Id = id;
        CriadoEm = criadoEm;
        ValorPago = valorPago;
        ValorAbatido = valorAbatido;
        Status = status;
    }

    public void RegistrarRecebimento(decimal valor)
    {
        valor = Math.Round(valor, 2, MidpointRounding.AwayFromZero);

        if (valor <= 0)
            throw new DomainException("Valor de recebimento deve ser maior que zero.");

        if (Status == StatusTituloFinanceiro.PAGO)
            throw new DomainException("Não é possível receber uma parcela que já está quitada.");

        if (Status == StatusTituloFinanceiro.CANCELADO)
            throw new DomainException("Não é possível receber uma parcela cancelada.");

        if (valor > ValorSaldo)
            throw new DomainException($"O valor informado (R$ {valor:F2}) excede o saldo restante da parcela (R$ {ValorSaldo:F2}).");

        ValorPago += valor;
        RecalcularStatus();
    }

    public void EstornarRecebimento(decimal valor)
    {
        valor = Math.Round(valor, 2, MidpointRounding.AwayFromZero);

        if (valor <= 0)
            throw new DomainException("Valor de estorno deve ser maior que zero.");

        if (Status == StatusTituloFinanceiro.CANCELADO)
            throw new DomainException("Não é possível estornar uma parcela cancelada.");

        if (valor > ValorPago)
            throw new DomainException($"O valor do estorno (R$ {valor:F2}) não pode ser maior do que o valor já recebido (R$ {ValorPago:F2}).");

        ValorPago -= valor;
        RecalcularStatus();
    }

    // Abate (devolução) até o saldo em aberto e devolve quanto foi abatido. O Valor original nunca muda;
    // parcela quitada só por abatimento fica PAGO.
    public decimal Abater(decimal valor)
    {
        if (Status == StatusTituloFinanceiro.CANCELADO) return 0;

        var abatido = Math.Min(Math.Round(valor, 2, MidpointRounding.AwayFromZero), ValorSaldo);
        if (abatido <= 0) return 0;

        ValorAbatido += abatido;
        RecalcularStatus();
        return abatido;
    }

    // Inverso de Abater (estorno de devolução): devolve até o que foi abatido; não mexe em ValorPago.
    public decimal Restituir(decimal valor)
    {
        if (Status == StatusTituloFinanceiro.CANCELADO) return 0;

        var restituido = Math.Min(Math.Round(valor, 2, MidpointRounding.AwayFromZero), ValorAbatido);
        if (restituido <= 0) return 0;

        ValorAbatido -= restituido;
        RecalcularStatus();
        return restituido;
    }

    private void RecalcularStatus()
        => Status = ValorSaldo <= 0 ? StatusTituloFinanceiro.PAGO
            : ValorPago + ValorAbatido > 0 ? StatusTituloFinanceiro.PARCIAL
            : StatusTituloFinanceiro.ABERTO;

    // Cancela a parcela; exige que nada tenha sido recebido.
    public void Cancelar()
    {
        if (PossuiBaixa)
            throw new DomainException("Não é possível cancelar uma conta a receber com parcelas baixadas.");

        Status = StatusTituloFinanceiro.CANCELADO;
    }

    public void Atualizar(string descricao, DateTime dataVencimento, decimal valor, string? observacao)
    {
        descricao = TextNormalization.Normalize(descricao);

        if (OrigemTipo != OrigemTituloFinanceiro.MANUAL)
            throw new DomainException("Conta gerada por outro documento não pode ser editada manualmente.");

        if (Status != StatusTituloFinanceiro.ABERTO || PossuiBaixa)
            throw new DomainException("Só é possível alterar uma parcela aberta e sem recebimentos.");

        if (string.IsNullOrWhiteSpace(descricao))
            throw new DomainException("Descrição da conta a receber é obrigatória.");

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
