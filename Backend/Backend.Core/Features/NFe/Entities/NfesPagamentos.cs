using Backend.Core.Common.Exceptions;
using Backend.Core.Features.NFe.Entities.Enums;
using Backend.Core.Features.Financeiro.Entities;

namespace Backend.Core.Features.NFe.Entities;

public class NfesPagamentos
{
    public int Id { get; set; }
    public int NfeId { get; set; }
    public IndicadorPagamento IndicadorPagamento { get; private set; }
    public decimal ValorPagamento { get; private set; }
    public MetodosPagamentos MetodosPagamento { get; private set; }

    public NfesPagamentos(
        IndicadorPagamento indicadorPagamento,
        decimal valorPagamento,
        MetodosPagamentos metodosPagamento)
    {
        if (valorPagamento <= 0)
            throw new DomainException("Valor do pagamento deve ser maior que zero.");

        IndicadorPagamento = indicadorPagamento;
        ValorPagamento = valorPagamento;
        MetodosPagamento = metodosPagamento ?? throw new DomainException("Método de pagamento é obrigatório.");
    }

    public void AtualizarMetodosPagamento(MetodosPagamentos metodosPagamento)
    {
        MetodosPagamento = metodosPagamento ?? throw new DomainException("Método de pagamento é obrigatório.");
    }
}

