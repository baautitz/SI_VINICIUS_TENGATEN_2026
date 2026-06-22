namespace Backend.Core.Features.Financeiro.Commands;

public record CriarCondicaoPagamentoParcelaCommand(
    int NumeroParcela,
    decimal Percentual,
    int PrazoDias
);

public record CriarCondicaoPagamentoCommand(
    string Descricao,
    string MetodoPagamentoCodigo,
    decimal EntradaMinimaPercentual,
    decimal DescontoPercentual,
    decimal AcrescimoPercentual,
    decimal MultaPercentual,
    decimal TaxaJurosPercentual,
    bool Ativo,
    IEnumerable<CriarCondicaoPagamentoParcelaCommand> Parcelas
);
