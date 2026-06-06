namespace Backend.Core.Features.Catalogo.DTOs;

public record SkusResumo(
    string Sku,
    string? GtinEan,
    decimal Preco,
    decimal Estoque,
    bool Ativo,
    decimal CustoMedio,
    decimal CustoUltimaCompra,
    bool PermiteDecimais,
    int ProdutoId,
    string ProdutoNome,
    string UnidadeMedidaSigla
);
