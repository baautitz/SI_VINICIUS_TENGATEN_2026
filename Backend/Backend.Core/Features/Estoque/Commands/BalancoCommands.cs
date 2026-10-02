using System.Collections.Generic;

namespace Backend.Core.Features.Estoque.Commands;

// Skus vazio/nulo = todos os SKUs ativos.
public record CriarBalancoCommand(int? UsuarioId, string? Observacao, List<string>? Skus);

public record ContagemItemCommand(string Sku, decimal? QuantidadeContada);

public record InformarContagemCommand(List<ContagemItemCommand> Itens);
