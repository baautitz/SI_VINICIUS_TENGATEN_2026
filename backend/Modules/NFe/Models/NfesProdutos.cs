namespace Modules.NFe.Models;

public class NfesProdutos
{
  public int Id { get; set; }
  public int NfeId { get; set; }
  public int NumeroItem { get; set; }
  public int SkuId { get; set; }
  public string DescricaoItem { get; set; } = null!;
  public int UnidadeMedidaId { get; set; }
  public decimal Quantidade { get; set; }
  public decimal ValorUnitario { get; set; }
  public decimal ValorDesconto { get; set; }
  public decimal ValorTotal { get; set; }

  public Nfes Nfe { get; set; } = null!;
  public Catalogo.Models.Skus Sku { get; set; } = null!;
  public UnidadeMedida.Models.UnidadesMedida UnidadeMedida { get; set; } = null!;
}