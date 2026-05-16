namespace backend.Modules.Pagamentos.Models;

public class CondicoesPagamentos
{
  public int Id { get; set; }
  public required string Descricao { get; set; }
  public decimal EntradaMinimaPercentual { get; set; }
  public decimal DescontoPercentual { get; set; }
  public decimal AcrescimoPercentual { get; set; }
  public decimal MultaPercentual { get; set; }
  public decimal TaxaJurosPercentual { get; set; }
  public bool Ativo { get; set; }

  public required MetodosPagamentos MetodoPagamento { get; set; }
  public required IEnumerable<CondicoesPagamentosParcelas> CondicoesPagamentosParcelas { get; set; }
}