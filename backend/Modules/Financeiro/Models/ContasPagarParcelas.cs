namespace Modules.Financeiro.Models;

public class ContasPagarParcelas
{
  public int Id { get; set; }
  public int ContaPagarId { get; set; }
  public int NumeroParcela { get; set; }
  public DateTime? DataVencimento { get; set; }
  public decimal ValorParcela { get; set; }
  public decimal ValorPago { get; set; }
  public StatusTituloFinanceiro Status { get; set; }

  public required ContasPagar ContasPagar { get; set; }
}