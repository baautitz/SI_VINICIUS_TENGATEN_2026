using Backend.Core.Common.ValueObjects;

namespace Backend.Core.Features.Parceiros.DTOs;

public record UpdateEmitenteDto(
    string NomeRazaoSocial,
    string CpfCnpj,
    string? ApelidoNomeFantasia,
    string? Endereco,
    int? BairroId,
    string? Telefone,
    string? Email,
    string? RgIe,
    string? InscricaoMunicipal,
    string? RegimeTributario,
    string? Observacao,
    bool Ativo
);
