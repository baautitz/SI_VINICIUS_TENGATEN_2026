namespace backend.Modules.Catalogo.Models;

public class Skus
{
  public required string Sku { get; set; }
  public string? GtinEan { get; set; }
  public decimal Preco { get; set; }
  public decimal Estoque { get; set; }
  public bool Ativo { get; set; }

  public required IEnumerable<SkusAtributosValores> SkusAtributosValores { get; set; }
}