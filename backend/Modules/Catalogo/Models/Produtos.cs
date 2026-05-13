namespace Modules.Catalogo.Models;

public class Produtos
{
  public int Id { get; set; }
  public required string Produto { get; set; }
  public required string Descricao { get; set; }
  public int CategoriaId { get; set; }
  public int MarcaId { get; set; }
  public int UnidadeMedidaId { get; set; }
  public bool Ativo { get; set; }

  public required Categorias Categoria { get; set; }
  public required Marcas Marca { get; set; }
  public required UnidadeMedida.Models.UnidadesMedida UnidadeMedida { get; set; }

  public ICollection<Skus> Skus { get; set; } = new List<Skus>();
}