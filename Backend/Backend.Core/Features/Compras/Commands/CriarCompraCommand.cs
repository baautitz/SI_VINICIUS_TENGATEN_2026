using Backend.Core.Features.Financeiro.Commands;

namespace Backend.Core.Features.Compras.Commands;

public record CriarCompraItemCommand(
    string Sku,
    decimal Quantidade,
    decimal ValorUnitario,
    decimal ValorDesconto,
    string? CodigoProdutoFornecedor
);

public record CriarCompraCommand(
    DateTime DataCompra,
    int EmitenteId,
    int FornecedorId,
    decimal ValorFrete,
    decimal ValorSeguro,
    decimal ValorOutrasDespesas,
    decimal ValorDesconto,
    decimal ValorFreteTerceiro,
    int? TransportadoraId,
    DateTime? VencimentoFreteTerceiro,
    string? Observacao,
    IEnumerable<CriarCompraItemCommand> Itens,
    int? CondicaoPagamentoId,
    IEnumerable<CriarParcelaPagarCommand>? Parcelas
);
