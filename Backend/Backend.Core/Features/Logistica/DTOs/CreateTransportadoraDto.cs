namespace Backend.Core.Features.Logistica.DTOs;

public record CreateTransportadoraDto(
    string NomeRazaosocial,
    string CpfCnpj,
    string? RgIe,
    string? ApelidoNomefantasia,
    string? Endereco,
    int? BairroId,
    string? Telefone,
    string? Email,
    string? Rntrc,
    string? Observacao,
    bool Ativo
);
