namespace backend.Modules.Estoque.DTOs;

public record MovimentacoesEstoquesResumo(
    int Id,
    DateTime DataMovimentacao,
    string TipoMovimentacao,
    string? Observacao
);
