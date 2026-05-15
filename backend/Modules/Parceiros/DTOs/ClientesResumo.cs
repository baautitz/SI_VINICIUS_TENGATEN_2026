namespace backend.Modules.Parceiros.DTOs;

public record ClientesResumo(
    int Id,
    string NomeRazaoSocial,
    string CpfCnpj,
    string? ApelidoNomeFantasia
);
