using System;
using System.Collections.Generic;
using Backend.Core.Features.Financeiro.Entities.Enums;

namespace Backend.Core.Features.Financeiro.Commands;

public record AtualizarParcelaReceberCommand(
    int NumeroParcela,
    DateTime DataVencimento,
    decimal ValorParcela,
    decimal ValorRecebido,
    StatusTituloFinanceiro Status
);

public record AtualizarContaReceberCommand(
    string Descricao,
    int ClienteId,
    int? NfeId,
    int? VendaId,
    DateTime? DataEmissao,
    decimal ValorOriginal,
    int? CondicaoPagamentoId,
    string? Observacao,
    IEnumerable<AtualizarParcelaReceberCommand> Parcelas
);
