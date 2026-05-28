namespace Backend.Core.Features.Parceiros.DTOs;

public record FornecedoresResumo(
    int Id,
    string NomeRazaosocial,
    string CpfCnpj,
    bool Ativo
);
