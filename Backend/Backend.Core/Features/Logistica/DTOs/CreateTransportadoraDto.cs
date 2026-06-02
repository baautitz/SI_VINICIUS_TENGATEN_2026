using Backend.Core.Common.Enums;
using Backend.Core.Common.ValueObjects;

namespace Backend.Core.Features.Logistica.DTOs;

public record CreateTransportadoraDto(
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
    string? Rntrc,
    string? Observacao,
    bool Ativo
);
