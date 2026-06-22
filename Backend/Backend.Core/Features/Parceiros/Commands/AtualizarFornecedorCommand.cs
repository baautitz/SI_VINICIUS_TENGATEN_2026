using Backend.Core.Features.Parceiros.Enums;

namespace Backend.Core.Features.Parceiros.Commands;

public record AtualizarFornecedorCommand(
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
    string? Observacao,
    bool Ativo
);

