namespace Backend.Core.Features.Pagamentos.Entities;

public class CondicoesPagamentosParcelas
{
    public int Id { get; set; }
    public int NumeroParcela { get; set; }
    public decimal Percentual { get; set; }
    public int PrazoDias { get; set; }
}