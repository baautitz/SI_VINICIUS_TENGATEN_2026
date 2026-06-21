namespace Backend.Core.Features.Financeiro.Commands;

public record EstornarRecebimentoParcelaCommand(
    int NumeroParcela,
    decimal ValorEstorno
);
