using System;
using System.Collections.Generic;

namespace Backend.Core.Features.Financeiro.Commands;

public record CriarParcelaReceberCommand(
    int NumeroParcela,
    DateTime DataVencimento,
    decimal ValorParcela
);

public record CriarContaReceberCommand(
    string Descricao,
    int ClienteId,
    DateTime? DataEmissao,
    decimal ValorOriginal,
    int? CondicaoPagamentoId,
    string? Observacao,
    IEnumerable<CriarParcelaReceberCommand> Parcelas
);
