using Backend.Core.Common.ValueObjects;

namespace Backend.Core.Features.Parceiros.DTOs;

public record EmitentesResumo(
    int Id,
    string NomeRazaoSocial,
    string CpfCnpj,
    string? ApelidoNomeFantasia
);
