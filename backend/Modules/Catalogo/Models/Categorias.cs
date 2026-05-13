namespace Modules.Catalogo.Models;

public class Categorias
{
  public int Id { get; set; }
  public string Categoria { get; set; } = null!;
  public string? Descricao { get; set; }
  public bool Ativo { get; set; }

  public ICollection<Produtos> Produtos { get; set; } = new List<Produtos>();
}