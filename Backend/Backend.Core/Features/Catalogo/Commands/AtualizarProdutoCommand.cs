using System.Collections.Generic;
namespace Backend.Core.Features.Catalogo.Commands;

public record AtualizarProdutoCommand(string Produto, string? Descricao, int CategoriaId, int MarcaId, int UnidadeMedidaId, bool Ativo, List<AtualizarSkuCommand> Skus);
public record AtualizarSkuCommand(string Sku, decimal Preco, string GtinEan, bool Ativo, List<int> AtributoValorIds);
