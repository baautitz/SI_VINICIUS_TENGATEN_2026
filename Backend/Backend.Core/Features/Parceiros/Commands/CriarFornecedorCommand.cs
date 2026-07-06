using Backend.Core.Features.Parceiros.Enums;

namespace Backend.Core.Features.Parceiros.Commands;

public record CriarFornecedorCommand(
    TipoPessoa TipoPessoa,
    string NomeRazaosocial,
    string CpfCnpj,
    string? RgIe,
    string? ApelidoNomefantasia,
    string? Logradouro,
    string? Numero,
    int? BairroId,
    int NacionalidadeId,
    string? Telefone,
    string? Email,
    string? Observacao,
    bool Ativo,
    string? Sexo = null,
    DateTime? DataNascimento = null
);

