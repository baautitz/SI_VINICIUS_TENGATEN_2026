namespace backend.Modules.Catalogo.Models;

public class SkusAtributosValores
{
  public required string Sku { get; set; }
  public int ChaveId { get; set; }
  public required string Valor { get; set; }

  public Skus? SkuEntity { get; set; }
  public SkuAtributosChaves? SkuAtributoChave { get; set; }
}
