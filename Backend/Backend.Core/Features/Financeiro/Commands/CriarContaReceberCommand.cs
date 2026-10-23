namespace Backend.Core.Features.Financeiro.Commands;

// Usado também pela venda.
public record CriarParcelaReceberCommand(
    int NumeroParcela,
    DateTime DataVencimento,
    decimal ValorParcela
);

public record ParcelaContaReceberCommand(DateTime DataVencimento, decimal Valor);

public record CriarContaReceberCommand(
    string Descricao,
    int ClienteId,
    string? NumeroDocumento,
    DateTime? DataEmissao,
    int? CondicaoPagamentoId,
    string? Observacao,
    IEnumerable<ParcelaContaReceberCommand> Parcelas
);
