using Backend.Core.Features.Estoque.Entities.Enums;

namespace Backend.Core.Features.Estoque.DTOs;

public record KardexLinha(
    int MovimentacaoId,
    string Sku,
    string ProdutoNome,
    DateTime DataMovimentacao,
    TipoMovimentacaoEstoque TipoMovimentacao,
    OrigemMovimentacaoEstoque OrigemTipo,
    int? OrigemId,
    string? Motivo,
    decimal Quantidade,
    decimal CustoUnitario,
    decimal QuantidadeAnterior,
    decimal QuantidadePosterior
);
