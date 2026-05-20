namespace Backend.Core.Features.Financeiro.DTOs;

public record ContasPagarResumo(
    int Id,
    string FornecedorNome,
    string Descricao,
    DateTime? DataVencimento,
    decimal ValorSaldo,
    string Status
);
