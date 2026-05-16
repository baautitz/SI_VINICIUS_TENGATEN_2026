namespace backend.Modules.Catalogo.DTOs;

public record SkusResumo(
    string Sku,
    string? GtinEan,
    decimal Preco,
    decimal Estoque,
    bool Ativo
);
