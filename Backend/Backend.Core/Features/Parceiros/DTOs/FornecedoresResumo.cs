namespace Backend.Core.Features.Parceiros.DTOs;

public record FornecedoresResumo(
    int Id,
    string NomeRazaoSocial,
    string CpfCnpj,
    string ApelidoNomeFantasia
);
