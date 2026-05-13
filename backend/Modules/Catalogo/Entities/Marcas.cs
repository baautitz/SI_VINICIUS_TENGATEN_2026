namespace Modules.Catalogo.Entities;

public class Marcas
{
  public int Id { get; set; }
  public string Marca { get; set; } = null!;
  public string? Descricao { get; set; }
  public bool Ativo { get; set; }

  public ICollection<Produtos> Produtos { get; set; } = new List<Produtos>();
}