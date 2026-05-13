namespace Modules.Financeiro.Entities;

public class ContasReceberParcelas
{
  public int Id { get; set; }
  public int ContaReceberId { get; set; }
  public int NumeroParcela { get; set; }
  public DateTime? DataVencimento { get; set; }
  public decimal ValorParcela { get; set; }
  public decimal ValorRecebido { get; set; }
  public StatusTituloFinanceiro Status { get; set; }

  public object ContaReceber { get; set; } = null!;
}