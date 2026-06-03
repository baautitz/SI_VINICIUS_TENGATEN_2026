namespace Backend.Core.Features.Catalogo.DTOs;

public record UpdateCategoriaDto(
    string Categoria,
    string? Descricao,
    bool Ativo
);
