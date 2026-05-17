namespace Backend.Core.Catalogo.DTOs;

public record SkusResumo(
    string Sku,
    string? GtinEan,
    decimal Preco,
    decimal Estoque,
    bool Ativo
);
