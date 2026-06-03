namespace Backend.Core.Features.Catalogo.DTOs;

public record UpdateMarcaDto(
    string Marca,
    string? Descricao,
    bool Ativo
);
