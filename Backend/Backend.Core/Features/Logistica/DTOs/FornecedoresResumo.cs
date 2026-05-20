namespace Backend.Core.Features.Logistica.DTOs;

public record FornecedoresResumo(
    int Id,
    string NomeRazaosocial,
    string CpfCnpj,
    bool Ativo
);
