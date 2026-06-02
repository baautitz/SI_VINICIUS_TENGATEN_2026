using Backend.Core.Common.Enums;
using Backend.Core.Common.ValueObjects;

namespace Backend.Core.Features.Parceiros.DTOs;

public record UpdateClienteDto(
    TipoPessoa TipoPessoa,
    string NomeRazaoSocial,
    string CpfCnpj,
    string? RgIe,
    string? ApelidoNomeFantasia,
    string? Endereco,
    int? BairroId,
    int NacionalidadeId,
    string? Telefone,
    string? Email,
    decimal LimiteCredito,
    string? Observacao,
    bool Ativo
);
