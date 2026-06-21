using Backend.Core.Common.Exceptions;
using Backend.Core.Features.Financeiro.Entities.Enums;

namespace Backend.Core.Features.Financeiro.Entities;

public class ContasReceberParcelas
{
    public int Id { get; private set; }
    public int ContaReceberId { get; private set; }

    protected ContasReceberParcelas() { }
    public int NumeroParcela { get; private set; }
    public DateTime DataVencimento { get; private set; }
    public decimal ValorParcela { get; private set; }
    public decimal ValorRecebido { get; private set; }
    public StatusTituloFinanceiro Status { get; private set; }

    public ContasReceberParcelas(int numeroParcela, DateTime dataVencimento, decimal valorParcela)
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
        ValorRecebido = 0;
        Status = StatusTituloFinanceiro.ABERTO;
    }

    public ContasReceberParcelas(int id, int contaReceberId, int numeroParcela, DateTime dataVencimento, decimal valorParcela, decimal valorRecebido, StatusTituloFinanceiro status)
        : this(numeroParcela, dataVencimento, valorParcela)
    {
        Id = id;
        ContaReceberId = contaReceberId;
        ValorRecebido = valorRecebido;
        Status = status;
    }

public void RegistrarRecebimento(decimal valor)
    {
        if (valor <= 0)
            throw new DomainException("Valor de recebimento deve ser maior que zero.");

        if (Status == StatusTituloFinanceiro.PAGO)
            throw new DomainException("Não é possível receber uma parcela que já está quitada.");

        if (Status == StatusTituloFinanceiro.CANCELADO)
            throw new DomainException("Não é possível receber uma parcela cancelada.");

        var saldoRestante = ValorParcela - ValorRecebido;
        if (valor > saldoRestante)
            throw new DomainException($"O valor informado (R$ {valor:F2}) excede o saldo restante da parcela (R$ {saldoRestante:F2}).");

        ValorRecebido += valor;

        if (ValorRecebido >= ValorParcela)
        {
            Status = StatusTituloFinanceiro.PAGO;
            ValorRecebido = ValorParcela;
        }
        else
        {
            Status = StatusTituloFinanceiro.PARCIAL;
        }
    }

    public void EstornarRecebimento(decimal valor)
    {
        if (valor <= 0)
            throw new DomainException("Valor de estorno deve ser maior que zero.");

        if (valor > ValorRecebido)
            throw new DomainException($"O valor do estorno (R$ {valor:F2}) não pode ser maior do que o valor já recebido (R$ {ValorRecebido:F2}).");

        if (Status == StatusTituloFinanceiro.CANCELADO)
            throw new DomainException("Não é possível estornar uma parcela cancelada.");

        ValorRecebido -= valor;

        if (ValorRecebido == 0)
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
