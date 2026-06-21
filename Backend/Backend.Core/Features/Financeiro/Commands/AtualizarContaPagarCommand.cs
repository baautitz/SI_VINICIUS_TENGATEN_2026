using System;
using System.Collections.Generic;
using Backend.Core.Features.Financeiro.Entities.Enums;

namespace Backend.Core.Features.Financeiro.Commands;

public record AtualizarParcelaPagarCommand(
    int NumeroParcela,
    DateTime DataVencimento,
    decimal ValorParcela,
    decimal ValorPago,
    StatusTituloFinanceiro Status
);

public record AtualizarContaPagarCommand(
    string Descricao,
    int FornecedorId,
    int? NfeId,
    DateTime? DataEmissao,
    decimal ValorOriginal,
    int? CondicaoPagamentoId,
    string? Observacao,
    IEnumerable<AtualizarParcelaPagarCommand> Parcelas
);
