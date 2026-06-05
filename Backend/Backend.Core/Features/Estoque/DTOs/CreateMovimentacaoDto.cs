using System.Collections.Generic;

namespace Backend.Core.Features.Estoque.DTOs;

public record CreateMovimentacaoDto(
    string TipoMovimentacao,
    int? UsuarioId,
    int? NfeId,
    int? VendaId,
    string? Observacao,
    List<CreateMovimentacaoItemDto> Itens
);

public record CreateMovimentacaoItemDto(
    string Sku,
    decimal Quantidade,
    decimal? CustoUnitario
);
