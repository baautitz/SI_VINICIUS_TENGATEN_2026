namespace Modules.UnidadeMedida.Models;

public class UnidadesMedida
{
  public int Id { get; set; }
  public string Sigla { get; set; } = null!;
  public string Descricao { get; set; } = null!;
  public string Categoria { get; set; } = null!;
  public bool Ativo { get; set; }

  public ICollection<Catalogo.Models.Produtos> Produtos { get; set; } = new List<Catalogo.Models.Produtos>();
}