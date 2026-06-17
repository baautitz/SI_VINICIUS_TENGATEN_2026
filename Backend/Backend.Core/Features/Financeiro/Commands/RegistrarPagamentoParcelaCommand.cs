namespace Backend.Core.Features.Financeiro.Commands;

public record RegistrarPagamentoParcelaCommand(
    int NumeroParcela,
    decimal ValorPago
);
