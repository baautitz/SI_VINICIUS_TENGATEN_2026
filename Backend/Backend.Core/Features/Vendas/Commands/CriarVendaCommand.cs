using System;
using System.Collections.Generic;
using Backend.Core.Features.Financeiro.Commands;

namespace Backend.Core.Features.Vendas.Commands;

public record CriarVendaItemCommand(
    string Sku,
    decimal Quantidade,
    decimal ValorUnitario,
    decimal ValorDesconto
);

public record CriarVendaCommand(
    DateTime DataVenda,
    int EmitenteId,
    int ClienteId,
    string? Observacao,
    IEnumerable<CriarVendaItemCommand> Itens,
    int? CondicaoPagamentoId,
    IEnumerable<CriarParcelaReceberCommand>? Parcelas
);
