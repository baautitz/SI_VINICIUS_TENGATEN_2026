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

// Uma forma de pagamento da venda: a condicao, quanto do total ela cobre e as parcelas que gera.
// Condicao a vista (entrada minima de 100%) nasce recebida.
public record CriarVendaPagamentoCommand(
    int CondicaoPagamentoId,
    decimal Valor,
    IEnumerable<CriarParcelaReceberCommand>? Parcelas
);

public record CriarVendaCommand(
    DateTime DataVenda,
    int EmitenteId,
    int ClienteId,
    string? Observacao,
    IEnumerable<CriarVendaItemCommand> Itens,
    IEnumerable<CriarVendaPagamentoCommand>? Pagamentos
);
