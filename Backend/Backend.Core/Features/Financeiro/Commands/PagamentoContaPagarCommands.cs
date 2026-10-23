namespace Backend.Core.Features.Financeiro.Commands;

public record PagarContaPagarCommand(decimal Valor);

public record EstornarContaPagarCommand(decimal Valor);
