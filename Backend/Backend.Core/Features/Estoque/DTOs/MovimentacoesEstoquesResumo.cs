namespace Backend.Core.Features.Estoque.DTOs;

public record MovimentacoesEstoquesResumo(
    int Id,
    DateTime DataMovimentacao,
    string TipoMovimentacao,
    string Status,
    string? Observacao,
    decimal ValorTotal
);
