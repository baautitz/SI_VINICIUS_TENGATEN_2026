namespace Backend.Core.Features.Catalogo.Commands;

public record CriarMarcaCommand(
    string Marca,
    string? Descricao
);
