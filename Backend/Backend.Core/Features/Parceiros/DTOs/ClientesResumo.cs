using Backend.Core.Common.Enums;

namespace Backend.Core.Features.Parceiros.DTOs;

public record ClientesResumo
{
    public int Id { get; init; }
    public TipoPessoa TipoPessoa { get; init; }
    public string NomeRazaoSocial { get; init; } = null!;
    public string CpfCnpj { get; init; } = null!;
    public string? ApelidoNomeFantasia { get; init; }
    public int NacionalidadeId { get; init; }

    public ClientesResumo() { }

    public ClientesResumo(int id, TipoPessoa tipoPessoa, string nomeRazaoSocial, string cpfCnpj, string? apelidoNomeFantasia, int nacionalidadeId)
    {
        Id = id;
        TipoPessoa = tipoPessoa;
        NomeRazaoSocial = nomeRazaoSocial;
        CpfCnpj = cpfCnpj;
        ApelidoNomeFantasia = apelidoNomeFantasia;
        NacionalidadeId = nacionalidadeId;
    }
}
