namespace Modules.NFe.Models;

public class NfesPagamentos
{
  public int Id { get; set; }
  public int NfeId { get; set; }
  public int MetodoPagamentoId { get; set; }
  public Enums.IndicadorPagamento IndicadorPagamento { get; set; }
  public decimal ValorPagamento { get; set; }

  public Nfes Nfe { get; set; } = null!;
  public Pagamentos.Models.MetodosPagamentos MetodosPagamento { get; set; } = null!;
}