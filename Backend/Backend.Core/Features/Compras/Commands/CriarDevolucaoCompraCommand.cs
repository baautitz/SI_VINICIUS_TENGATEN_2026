namespace Backend.Core.Features.Compras.Commands;

public record CriarDevolucaoCompraItemCommand(int CompraItemId, decimal Quantidade);

public record CriarDevolucaoCompraCommand(string Motivo, IEnumerable<CriarDevolucaoCompraItemCommand> Itens);
