namespace Modules.Catalogo.Models;

public class Marcas
{
  public int Id { get; set; }
  public required string Marca { get; set; }
  public string? Descricao { get; set; }
  public bool Ativo { get; set; }

  public ICollection<Produtos> Produtos { get; set; } = new List<Produtos>();
}