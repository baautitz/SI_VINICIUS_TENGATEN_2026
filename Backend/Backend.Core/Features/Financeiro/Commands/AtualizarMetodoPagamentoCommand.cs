namespace Backend.Core.Features.Financeiro.Commands;

public record AtualizarMetodoPagamentoCommand(
    string Descricao,
    bool Ativo
);
