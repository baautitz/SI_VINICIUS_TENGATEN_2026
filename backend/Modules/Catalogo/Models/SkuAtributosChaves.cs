namespace backend.Modules.Catalogo.Models;

public class SkuAtributosChaves
{
  public int Id { get; set; }
  public required string Chave { get; set; }

  public required IEnumerable<SkusAtributosValores> SkusAtributosValores { get; set; }
}
