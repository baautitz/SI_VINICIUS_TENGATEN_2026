namespace Backend.Core.Features.Parceiros.DTOs;

public record UpdateFornecedorDto(
    string NomeRazaosocial,
    string CpfCnpj,
    string? RgIe,
    string? ApelidoNomefantasia,
    string? Endereco,
    int? BairroId,
    string? Telefone,
    string? Email,
    string? Observacao,
    bool Ativo
);
