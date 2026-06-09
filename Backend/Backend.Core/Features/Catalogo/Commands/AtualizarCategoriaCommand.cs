namespace Backend.Core.Features.Catalogo.Commands;

public record AtualizarCategoriaCommand(
    string Categoria,
    string? Descricao,
    bool Ativo
);
