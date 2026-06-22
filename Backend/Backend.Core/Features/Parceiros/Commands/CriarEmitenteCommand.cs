using Backend.Core.Features.Parceiros.Enums;

namespace Backend.Core.Features.Parceiros.Commands;

public record CriarEmitenteCommand(
    TipoPessoa TipoPessoa,
    string NomeRazaoSocial,
    string CpfCnpj,
    string? ApelidoNomeFantasia,
    string? Endereco,
    int? BairroId,
    int NacionalidadeId,
    string? Telefone,
    string? Email,
    string? RgIe,
    string? InscricaoMunicipal,
    string? RegimeTributario,
    string? Observacao,
    bool Ativo
);

