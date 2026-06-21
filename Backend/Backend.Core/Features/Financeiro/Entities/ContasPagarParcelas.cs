using Backend.Core.Common.Exceptions;
using Backend.Core.Features.Financeiro.Entities.Enums;

namespace Backend.Core.Features.Financeiro.Entities;

public class ContasPagarParcelas
{
    public int Id { get; private set; }
    public int ContaPagarId { get; private set; }

    protected ContasPagarParcelas() { }
    public int NumeroParcela { get; private set; }
    public DateTime DataVencimento { get; private set; }
    public decimal ValorParcela { get; private set; }
    public decimal ValorPago { get; private set; }
    public StatusTituloFinanceiro Status { get; private set; }

    public ContasPagarParcelas(int numeroParcela, DateTime dataVencimento, decimal valorParcela)
    {
        if (numeroParcela <= 0)
            throw new DomainException("Número da parcela deve ser maior que zero.");

        if (dataVencimento == default)
            throw new DomainException("Data de vencimento é obrigatória.");

        if (valorParcela <= 0)
            throw new DomainException("Valor da parcela deve ser maior que zero.");

        NumeroParcela = numeroParcela;
        DataVencimento = dataVencimento;
        ValorParcela = valorParcela;
        ValorPago = 0;
        Status = StatusTituloFinanceiro.ABERTO;
    }

    public ContasPagarParcelas(int id, int contaPagarId, int numeroParcela, DateTime dataVencimento, decimal valorParcela, decimal valorPago, StatusTituloFinanceiro status)
        : this(numeroParcela, dataVencimento, valorParcela)
    {
        Id = id;
        ContaPagarId = contaPagarId;
        ValorPago = valorPago;
        Status = status;
    }

    public void RegistrarPagamento(decimal valor)
    {
        if (valor <= 0)
            throw new DomainException("Valor de pagamento deve ser maior que zero.");

        if (Status == StatusTituloFinanceiro.PAGO)
            throw new DomainException("Não é possível pagar uma parcela que já está quitada.");

        if (Status == StatusTituloFinanceiro.CANCELADO)
            throw new DomainException("Não é possível pagar uma parcela cancelada.");

        var saldoRestante = ValorParcela - ValorPago;
        if (valor > saldoRestante)
            throw new DomainException($"O valor informado (R$ {valor:F2}) excede o saldo restante da parcela (R$ {saldoRestante:F2}).");

        ValorPago += valor;

        if (ValorPago >= ValorParcela)
        {
            Status = StatusTituloFinanceiro.PAGO;
            ValorPago = ValorParcela;
        }
        else
        {
            Status = StatusTituloFinanceiro.PARCIAL;
        }
    }

    public void EstornarPagamento(decimal valor)
    {
        if (valor <= 0)
            throw new DomainException("Valor de estorno deve ser maior que zero.");

        if (valor > ValorPago)
            throw new DomainException($"O valor do estorno (R$ {valor:F2}) não pode ser maior do que o valor já pago (R$ {ValorPago:F2}).");

        if (Status == StatusTituloFinanceiro.CANCELADO)
            throw new DomainException("Não é possível estornar uma parcela cancelada.");

        ValorPago -= valor;

        if (ValorPago == 0)
        {
            Status = StatusTituloFinanceiro.ABERTO;
        }
        else
        {
            Status = StatusTituloFinanceiro.PARCIAL;
        }
    }

    public void Cancelar()
    {
        Status = StatusTituloFinanceiro.CANCELADO;
    }
}
