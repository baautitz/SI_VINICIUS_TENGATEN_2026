using Backend.Core.Features.Financeiro.Commands;

namespace Backend.Core.Features.Compras.Commands;

public record CriarCompraItemCommand(
    string Sku,
    decimal Quantidade,
    decimal ValorUnitario,
    decimal ValorDesconto,
    string? CodigoProdutoFornecedor,
    // vProd do XML: quando informado, é o valor bruto do item (qtd x unit pode ter mais casas que o documento).
    decimal? ValorBruto = null
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
