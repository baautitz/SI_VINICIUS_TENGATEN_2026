namespace Backend.Core.Features.Financeiro.Commands;

public record EstornarPagamentoParcelaCommand(
    int NumeroParcela,
    decimal ValorEstorno
);
