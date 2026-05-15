namespace backend.Modules.Catalogo.DTOs;

public record SkusResumo(
    int Id,
    string Variante,
    string? Sku,
    string? GtinEan,
    decimal Preco,
    decimal Estoque,
    bool Ativo
);
