namespace backend.Modules.Logistica.DTOs;

public record FornecedoresResumo(
    int Id,
    string NomeRazaosocial,
    string CpfCnpj,
    bool Ativo
);
