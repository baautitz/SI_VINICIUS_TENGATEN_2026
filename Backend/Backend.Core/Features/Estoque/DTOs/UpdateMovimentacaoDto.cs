using System.Collections.Generic;

namespace Backend.Core.Features.Estoque.DTOs;

public record UpdateMovimentacaoDto(
    string TipoMovimentacao,
    int? UsuarioId,
    int? NfeId,
    int? VendaId,
    string? Observacao,
    List<UpdateMovimentacaoItemDto> Itens
);

public record UpdateMovimentacaoItemDto(
    string Sku,
    decimal Quantidade,
    decimal? CustoUnitario
);
