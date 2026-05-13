namespace Modules.Catalogo.Models;

public class Skus
{
  public int Id { get; set; }
  public int ProdutoId { get; set; }
  public required string Variante { get; set; }
  public string? Caracteristicas { get; set; }
  public string? Sku { get; set; }
  public string? GtinEan { get; set; }
  public decimal Preco { get; set; }
  public decimal Estoque { get; set; }
  public bool Ativo { get; set; }

  public required Produtos Produto { get; set; }
}