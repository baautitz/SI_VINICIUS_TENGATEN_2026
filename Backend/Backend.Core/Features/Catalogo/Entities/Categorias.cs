namespace Backend.Core.Features.Catalogo.Entities;

public class Categorias
{
    public int Id { get; set; }
    public required string Categoria { get; set; }
    public string? Descricao { get; set; }
    public bool Ativo { get; set; }
}