using Backend.Core.NFe.Entities.Enums;
using Backend.Core.Pagamentos.Entities;

namespace Backend.Core.NFe.Entities;

public class NfesPagamentos
{
  public int Id { get; set; }
  public IndicadorPagamento IndicadorPagamento { get; set; }
  public decimal ValorPagamento { get; set; }

  public required MetodosPagamentos MetodosPagamento { get; set; }
}