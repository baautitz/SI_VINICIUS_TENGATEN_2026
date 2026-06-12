namespace Backend.Core.Features.Pagamentos.Commands;

public record AtualizarCondicaoPagamentoParcelaCommand(
    int NumeroParcela,
    decimal Percentual,
    int PrazoDias
);

public record AtualizarCondicaoPagamentoCommand(
    string Descricao,
    int MetodoPagamentoId,
    decimal EntradaMinimaPercentual,
    decimal DescontoPercentual,
    decimal AcrescimoPercentual,
    decimal MultaPercentual,
    decimal TaxaJurosPercentual,
    bool Ativo,
    IEnumerable<AtualizarCondicaoPagamentoParcelaCommand> Parcelas
);
