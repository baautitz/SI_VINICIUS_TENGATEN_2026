using System.Collections.Generic;
namespace Backend.Core.Features.Catalogo.Commands;

public record CriarProdutoCommand(string Produto, string? Descricao, int CategoriaId, int MarcaId, int UnidadeMedidaId, bool Ativo, List<CriarSkuCommand> Skus);
public record CriarSkuCommand(string? Sku, decimal Preco, string GtinEan, bool Ativo, List<int> AtributoValorIds);
