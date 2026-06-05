using System.Collections.Generic;

namespace Backend.Core.Features.Catalogo.DTOs;

public record UpdateProdutoDto(
    string Produto,
    string? Descricao,
    int? CategoriaId,
    int? MarcaId,
    int? UnidadeMedidaId,
    bool Ativo,
    List<UpdateSkuDto> Skus
);

public record UpdateSkuDto(
    string Sku,
    decimal Preco,
    decimal Estoque,
    string? GtinEan,
    bool Ativo,
    List<int> AtributoValorIds
);
