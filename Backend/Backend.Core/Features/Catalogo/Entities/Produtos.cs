using Backend.Core.Features.UnidadeMedida.Entities;

namespace Backend.Core.Features.Catalogo.Entities;

public class Produtos
{
    public int Id { get; set; }
    public required string Produto { get; set; }
    public required string Descricao { get; set; }
    public bool Ativo { get; set; }

    public required Categorias Categoria { get; set; }
    public required Marcas Marca { get; set; }
    public required UnidadesMedida UnidadeMedida { get; set; }

    public required IEnumerable<Skus> Skus { get; set; }
}