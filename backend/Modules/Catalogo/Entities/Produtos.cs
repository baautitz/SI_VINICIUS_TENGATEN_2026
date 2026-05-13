namespace Modules.Catalogo.Entities;

public class Produtos
{
  public int Id { get; set; }
  public string Produto { get; set; } = null!;
  public string Descricao { get; set; } = null!;
  public int CategoriaId { get; set; }
  public int MarcaId { get; set; }
  public int UnidadeMedidaId { get; set; }
  public bool Ativo { get; set; }

  public Categorias Categoria { get; set; } = null!;
  public Marcas Marca { get; set; } = null!;
  public UnidadeMedida.Entities.UnidadesMedida UnidadeMedida { get; set; } = null!;

  public ICollection<Skus> Skus { get; set; } = new List<Skus>();
}