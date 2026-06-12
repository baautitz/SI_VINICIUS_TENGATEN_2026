using Backend.Core.Common.Exceptions;
namespace Backend.Core.Features.Pagamentos.Entities;

public class CondicoesPagamentosParcelas
{
    public int Id { get; set; }
    public int NumeroParcela { get; private set; }
    public decimal Percentual { get; private set; }
    public int PrazoDias { get; private set; }

    protected CondicoesPagamentosParcelas() { }

    public CondicoesPagamentosParcelas(int numeroParcela, decimal percentual, int prazoDias)
    {
        if (numeroParcela <= 0)
            throw new DomainException("Número da parcela deve ser maior que zero.");

        if (percentual <= 0 || percentual > 100)
            throw new DomainException("Percentual da parcela deve estar entre 0 e 100.");

        if (prazoDias < 0)
            throw new DomainException("Prazo em dias não pode ser negativo.");

        NumeroParcela = numeroParcela;
        Percentual = percentual;
        PrazoDias = prazoDias;
    }

    public void AtualizarPercentual(decimal percentual)
    {
        if (percentual <= 0 || percentual > 100)
            throw new DomainException("Percentual da parcela deve estar entre 0 e 100.");

        Percentual = percentual;
    }

    public void AtualizarPrazo(int prazoDias)
    {
        if (prazoDias < 0)
            throw new DomainException("Prazo em dias não pode ser negativo.");

        PrazoDias = prazoDias;
    }
}
