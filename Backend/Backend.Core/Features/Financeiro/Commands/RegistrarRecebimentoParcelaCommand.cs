namespace Backend.Core.Features.Financeiro.Commands;

public record RegistrarRecebimentoParcelaCommand(
    int NumeroParcela,
    decimal ValorRecebido
);
