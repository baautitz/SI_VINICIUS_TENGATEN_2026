using Backend.Core.Common.ValueObjects;

namespace Backend.Core.Features.Parceiros.DTOs;

public record CreateFornecedorDto(
    string NomeRazaosocial,
    string CpfCnpj,
    string? RgIe,
    string? ApelidoNomefantasia,
    string? Endereco,
    int? BairroId,
    string? Telefone,
    string? Email,
    string? Observacao
);
