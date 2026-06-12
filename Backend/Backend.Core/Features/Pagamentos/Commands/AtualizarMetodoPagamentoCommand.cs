namespace Backend.Core.Features.Pagamentos.Commands;

public record AtualizarMetodoPagamentoCommand(
    string Descricao,
    bool Ativo
);
