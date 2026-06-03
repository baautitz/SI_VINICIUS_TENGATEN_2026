namespace Backend.Core.Features.Catalogo.DTOs;

public record CreateCategoriaDto(
    string Categoria,
    string? Descricao
);
