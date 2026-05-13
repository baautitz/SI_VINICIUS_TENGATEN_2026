namespace Modules.UnidadeMedida.Entities;

public class UnidadesMedida
{
  public int Id { get; set; }
  public string Sigla { get; set; } = null!;
  public string Descricao { get; set; } = null!;
  public string Categoria { get; set; } = null!;
  public bool Ativo { get; set; }

  public ICollection<Catalogo.Entities.Produtos> Produtos { get; set; } = new List<Catalogo.Entities.Produtos>();
}