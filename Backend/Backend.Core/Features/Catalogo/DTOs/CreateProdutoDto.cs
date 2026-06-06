using System.Collections.Generic;

namespace Backend.Core.Features.Catalogo.DTOs;

public record CreateProdutoDto(
    string Produto,
    string? Descricao,
    int? CategoriaId,
    int? MarcaId,
    int? UnidadeMedidaId,
    bool Ativo,
    List<CreateSkuDto> Skus
);

public record CreateSkuDto(
    string Sku,
    decimal Preco,
    string? GtinEan,
    bool Ativo,
    List<int> AtributoValorIds
);
