using backend.Modules.Pagamentos.Models;

namespace backend.Modules.NFe.Models;

public class NfesPagamentos
{
  public int Id { get; set; }
  public Enums.IndicadorPagamento IndicadorPagamento { get; set; }
  public decimal ValorPagamento { get; set; }

  public required MetodosPagamentos MetodosPagamento { get; set; }
}