namespace Backend.Core.Features.Financeiro.Commands;

public record CriarMetodoPagamentoCommand(
    string? Codigo,
    string Descricao,
    bool Ativo
);
