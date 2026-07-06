using Backend.Core.Features.Parceiros.Enums;

namespace Backend.Core.Features.Parceiros.Commands;

public record AtualizarClienteCommand(
    TipoPessoa TipoPessoa,
    string NomeRazaoSocial,
    string CpfCnpj,
    string? RgIe,
    string? ApelidoNomeFantasia,
    string? Logradouro,
    string? Numero,
    int? BairroId,
    int NacionalidadeId,
    string? Telefone,
    string? Email,
    decimal LimiteCredito,
    string? Observacao,
    bool Ativo,
    string? Sexo = null,
    DateTime? DataNascimento = null
);

