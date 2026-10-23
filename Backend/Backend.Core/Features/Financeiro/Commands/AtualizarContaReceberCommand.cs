namespace Backend.Core.Features.Financeiro.Commands;

// Edita UMA parcela (linha) manual.
public record AtualizarContaReceberCommand(
    string Descricao,
    DateTime DataVencimento,
    decimal Valor,
    string? Observacao
);
