namespace Modules.Catalogo.Models;

public class Skus
{
  public int Id { get; set; }
  public int ProdutoId { get; set; }
  public string Variante { get; set; } = null!;
  public string? Caracteristicas { get; set; }
  public string? Sku { get; set; }
  public string? GtinEan { get; set; }
  public decimal Preco { get; set; }
  public decimal Estoque { get; set; }
  public bool Ativo { get; set; }

  public Produtos Produto { get; set; } = null!;
}