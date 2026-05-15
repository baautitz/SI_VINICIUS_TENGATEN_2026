namespace backend.Modules.Parceiros.DTOs;

public record EmitentesResumo(
    int Id,
    string NomeRazaoSocial,
    string CpfCnpj,
    string? ApelidoNomeFantasia
);
