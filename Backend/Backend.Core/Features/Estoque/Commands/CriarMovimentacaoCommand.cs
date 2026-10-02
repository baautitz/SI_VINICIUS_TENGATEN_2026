using System.Collections.Generic;

namespace Backend.Core.Features.Estoque.Commands;

// Lançamento manual: a origem é sempre MANUAL e o motivo é obrigatório.
public record CriarMovimentacaoCommand(
    string TipoMovimentacao,
    int? UsuarioId,
    string Motivo,
    string? Observacao,
    List<MovimentacaoItemCommand> Itens
);

public record MovimentacaoItemCommand(
    string Sku,
    decimal Quantidade,
    decimal? CustoUnitario
);
