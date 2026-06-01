namespace Backend.Core.Features.Parceiros.DTOs;

public record UpdateClienteDto(
    string NomeRazaoSocial,
    string CpfCnpj,
    string? RgIe,
    string? ApelidoNomeFantasia,
    string? Endereco,
    int? BairroId,
    string? Telefone,
    string? Email,
    decimal LimiteCredito,
    string? Observacao,
    bool Ativo
);
