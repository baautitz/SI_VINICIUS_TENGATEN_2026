namespace backend.Modules.Catalogo.Models;

public class Categorias
{
  public int Id { get; set; }
  public required string Categoria { get; set; }
  public string? Descricao { get; set; }
  public bool Ativo { get; set; }
}