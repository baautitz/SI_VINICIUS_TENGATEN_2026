namespace backend.Modules.Financeiro.DTOs;

public record ContasReceberResumo(
    int Id,
    string ClienteNome,
    string Descricao,
    DateTime? DataVencimento,
    decimal ValorSaldo,
    string Status
);
