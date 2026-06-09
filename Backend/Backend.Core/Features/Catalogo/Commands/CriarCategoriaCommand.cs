namespace Backend.Core.Features.Catalogo.Commands;

public record CriarCategoriaCommand(
    string Categoria,
    string? Descricao
);
