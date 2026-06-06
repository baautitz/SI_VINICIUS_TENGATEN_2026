using System.Collections.Generic;

namespace Backend.Core.Features.Estoque.Commands;

public record AtualizarMovimentacaoCommand(
    string TipoMovimentacao,
    int? UsuarioId,
    int? NfeId,
    int? VendaId,
    string? Observacao,
    List<MovimentacaoItemCommand> Itens
);
