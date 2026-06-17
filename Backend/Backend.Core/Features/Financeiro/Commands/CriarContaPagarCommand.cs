using System;
using System.Collections.Generic;

namespace Backend.Core.Features.Financeiro.Commands;

public record CriarParcelaPagarCommand(
    int NumeroParcela,
    DateTime DataVencimento,
    decimal ValorParcela
);

public record CriarContaPagarCommand(
    string Descricao,
    int FornecedorId,
    int? NfeId,
    DateTime? DataEmissao,
    DateTime? DataVencimento,
    decimal ValorOriginal,
    int? CondicaoPagamentoId,
    string? Observacao,
    IEnumerable<CriarParcelaPagarCommand> Parcelas
);
