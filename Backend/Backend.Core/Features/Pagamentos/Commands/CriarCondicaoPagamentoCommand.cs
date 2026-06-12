namespace Backend.Core.Features.Pagamentos.Commands;

public record CriarCondicaoPagamentoParcelaCommand(
    int NumeroParcela,
    decimal Percentual,
    int PrazoDias
);

public record CriarCondicaoPagamentoCommand(
    string Descricao,
    int MetodoPagamentoId,
    decimal EntradaMinimaPercentual,
    decimal DescontoPercentual,
    decimal AcrescimoPercentual,
    decimal MultaPercentual,
    decimal TaxaJurosPercentual,
    bool Ativo,
    IEnumerable<CriarCondicaoPagamentoParcelaCommand> Parcelas
);
