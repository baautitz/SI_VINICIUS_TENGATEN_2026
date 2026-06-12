namespace Backend.Core.Features.Pagamentos.Commands;

public record CriarMetodoPagamentoCommand(
    string? Codigo,
    string Descricao,
    bool Ativo
);
