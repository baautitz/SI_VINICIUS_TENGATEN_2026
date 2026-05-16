namespace backend.Modules.Financeiro.Models;

public class ContasReceberParcelas
{
  public int Id { get; set; }
  public int NumeroParcela { get; set; }
  public DateTime? DataVencimento { get; set; }
  public decimal ValorParcela { get; set; }
  public decimal ValorRecebido { get; set; }
  public Enums.StatusTituloFinanceiro Status { get; set; }
}