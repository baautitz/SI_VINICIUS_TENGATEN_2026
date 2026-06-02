using Backend.Core.Common.Enums;
using Backend.Core.Common.ValueObjects;

namespace Backend.Core.Features.Parceiros.DTOs;

public record CreateFornecedorDto(
    TipoPessoa TipoPessoa,
    string NomeRazaosocial,
    string CpfCnpj,
    string? RgIe,
    string? ApelidoNomefantasia,
    string? Endereco,
    int? BairroId,
    int NacionalidadeId,
    string? Telefone,
    string? Email,
    string? Observacao
);
