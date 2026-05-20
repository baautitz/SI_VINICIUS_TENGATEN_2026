namespace Backend.Core.Features.Catalogo.Entities;

public class SkuAtributosChaves
{
    public int Id { get; set; }
    public required string Chave { get; set; }

    public required IEnumerable<SkusAtributosValores> SkusAtributosValores { get; set; }
}
