namespace Backend.Core.Features.Catalogo.Commands;

public record AtualizarMarcaCommand(
    string Marca,
    string? Descricao,
    bool Ativo
);
