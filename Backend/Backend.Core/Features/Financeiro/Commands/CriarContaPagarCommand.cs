namespace Backend.Core.Features.Financeiro.Commands;

// Usado também por compras/importação de NF-e.
public record CriarParcelaPagarCommand(
    int NumeroParcela,
    DateTime DataVencimento,
    decimal ValorParcela
);

public record ParcelaContaPagarCommand(DateTime DataVencimento, decimal Valor);

public record CriarContaPagarCommand(
    string Descricao,
    int FornecedorId,
    string? NumeroDocumento,
    DateTime? DataEmissao,
    int? CondicaoPagamentoId,
    string? Observacao,
    IEnumerable<ParcelaContaPagarCommand> Parcelas
);
