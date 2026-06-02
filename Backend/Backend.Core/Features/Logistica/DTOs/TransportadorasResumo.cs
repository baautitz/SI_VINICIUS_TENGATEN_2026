using Backend.Core.Common.Enums;

namespace Backend.Core.Features.Logistica.DTOs;

public record TransportadorasResumo
{
    public int Id { get; init; }
    public TipoPessoa TipoPessoa { get; init; }
    public string NomeRazaoSocial { get; init; } = null!;
    public string CpfCnpj { get; init; } = null!;
    public int NacionalidadeId { get; init; }
    public bool Ativo { get; init; }

    public TransportadorasResumo() { }

    public TransportadorasResumo(int id, TipoPessoa tipoPessoa, string nomeRazaoSocial, string cpfCnpj, int nacionalidadeId, bool ativo)
    {
        Id = id;
        TipoPessoa = tipoPessoa;
        NomeRazaoSocial = nomeRazaoSocial;
        CpfCnpj = cpfCnpj;
        NacionalidadeId = nacionalidadeId;
        Ativo = ativo;
    }
}
