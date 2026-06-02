using Backend.Core.Common.Helpers;
namespace Backend.Core.Features.NFe.Entities;

public class NfesInformacoesAdicionais
{
    public int Id { get; set; }
    public string? InformacoesFisco { get; private set; }
    public string? InformacoesComplementares { get; private set; }

    public NfesInformacoesAdicionais(string? informacoesFisco = null, string? informacoesComplementares = null)
    {
        InformacoesFisco = TextNormalization.NormalizeOrNull(informacoesFisco);
        InformacoesComplementares = TextNormalization.NormalizeOrNull(informacoesComplementares);
    }

    public void AtualizarInformacoes(string? informacoesFisco, string? informacoesComplementares)
    {
        InformacoesFisco = TextNormalization.NormalizeOrNull(informacoesFisco);
        InformacoesComplementares = TextNormalization.NormalizeOrNull(informacoesComplementares);
    }
}
