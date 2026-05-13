namespace Modules.Pagamentos.Entities;

public class CondicoesPagamentos
{
  public int Id { get; set; }
  public string Descricao { get; set; } = null!;
  public int MetodoPagamentoId { get; set; }
  public decimal EntradaMinimaPercentual { get; set; }
  public decimal DescontoPercentual { get; set; }
  public decimal AcrescimoPercentual { get; set; }
  public decimal MultaPercentual { get; set; }
  public decimal TaxaJurosPercentual { get; set; }
  public bool Ativo { get; set; }

  public MetodosPagamentos MetodoPagamento { get; set; } = null!;
  public ICollection<CondicoesPagamentosParcelas> CondicoesPagamentosParcelas { get; set; } = new List<CondicoesPagamentosParcelas>();
}