using System.Collections.Generic;

namespace Backend.Core.Features.Estoque.Commands;

public record CriarMovimentacaoCommand(
    string TipoMovimentacao,
    int? UsuarioId,
    int? NfeId,
    int? VendaId,
    string? Observacao,
    List<MovimentacaoItemCommand> Itens
);

public record MovimentacaoItemCommand(
    string Sku,
    decimal Quantidade,
    decimal? CustoUnitario
);
