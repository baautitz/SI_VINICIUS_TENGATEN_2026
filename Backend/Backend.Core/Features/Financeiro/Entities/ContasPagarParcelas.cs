using Backend.Core.Features.Financeiro.Entities.Enums;

namespace Backend.Core.Features.Financeiro.Entities;

public class ContasPagarParcelas
{
    public int Id { get; set; }
    public int NumeroParcela { get; set; }
    public DateTime? DataVencimento { get; set; }
    public decimal ValorParcela { get; set; }
    public decimal ValorPago { get; set; }
    public StatusTituloFinanceiro Status { get; set; }
}