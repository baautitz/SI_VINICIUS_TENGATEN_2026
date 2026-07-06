using Backend.Core.Features.Parceiros.Enums;

namespace Backend.Core.Features.Parceiros.Commands;

public record CriarTransportadoraCommand(
    TipoPessoa TipoPessoa,
    string NomeRazaosocial,
    string CpfCnpj,
    int NacionalidadeId,
    string? RgIe = null,
    string? ApelidoNomefantasia = null,
    string? Logradouro = null,
    string? Numero = null,
    int? BairroId = null,
    string? Telefone = null,
    string? Email = null,
    string? Rntrc = null,
    string? Observacao = null,
    bool Ativo = true
);

public record AtualizarTransportadoraCommand(
    TipoPessoa TipoPessoa,
    string NomeRazaosocial,
    string CpfCnpj,
    int NacionalidadeId,
    string? RgIe = null,
    string? ApelidoNomefantasia = null,
    string? Logradouro = null,
    string? Numero = null,
    int? BairroId = null,
    string? Telefone = null,
    string? Email = null,
    string? Rntrc = null,
    string? Observacao = null,
    bool Ativo = true
);
