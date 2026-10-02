namespace Backend.Core.Features.Vendas.Commands;

public record CriarDevolucaoItemCommand(int VendaItemId, decimal Quantidade);

public record CriarDevolucaoCommand(string Motivo, IEnumerable<CriarDevolucaoItemCommand> Itens);
