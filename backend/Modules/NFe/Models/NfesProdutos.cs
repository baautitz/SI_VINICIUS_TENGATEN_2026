namespace Modules.NFe.Models;

public class NfesProdutos
{
  public int Id { get; set; }
  public int NfeId { get; set; }
  public int NumeroItem { get; set; }
  public int SkuId { get; set; }
  public required string DescricaoItem { get; set; }
  public int UnidadeMedidaId { get; set; }
  public decimal Quantidade { get; set; }
  public decimal ValorUnitario { get; set; }
  public decimal ValorDesconto { get; set; }
  public decimal ValorTotal { get; set; }

  public required Nfes Nfe { get; set; }
  public required Catalogo.Models.Skus Sku { get; set; }
  public required UnidadeMedida.Models.UnidadesMedida UnidadeMedida { get; set; }
}