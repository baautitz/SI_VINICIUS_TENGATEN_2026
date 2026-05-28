using Backend.Core.Common;
using Backend.Core.Features.Financeiro.Entities.Enums;

namespace Backend.Core.Features.Financeiro.Entities;

public class ContasPagarParcelas
{
    public int Id { get; private set; }
    public int ContaPagarId { get; private set; }
    public int NumeroParcela { get; private set; }
    public DateTime DataVencimento { get; private set; }
    public decimal ValorParcela { get; private set; }
    public decimal ValorPago { get; private set; }
    public StatusTituloFinanceiro Status { get; private set; }

    public ContasPagarParcelas(int numeroParcela, DateTime dataVencimento, decimal valorParcela)
    {
        if (numeroParcela <= 0)
            throw new DomainException("Número da parcela deve ser maior que zero.");

        if (dataVencimento <= DateTime.UtcNow.Date)
            throw new DomainException("Data de vencimento deve ser futura.");

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

        if (Status == StatusTituloFinanceiro.CANCELADO)
            throw new DomainException("Não é possível pagar uma parcela cancelada.");

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

    public void Cancelar()
    {
        Status = StatusTituloFinanceiro.CANCELADO;
    }
}
