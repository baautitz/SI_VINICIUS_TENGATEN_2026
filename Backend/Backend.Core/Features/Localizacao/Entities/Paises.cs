using Backend.Core.Common;

namespace Backend.Core.Features.Localizacao.Entities;

public class Paises
{
    public int Id { get; private set; }
    public string Ddi { get; private set; } = null!;
    public string SiglaIso { get; private set; } = null!;
    public string Moeda { get; private set; } = null!;
    public string SimboloMoeda { get; private set; } = null!;
    public string Pais { get; private set; } = null!;

    public Paises(string ddi, string siglaIso, string moeda, string simboloMoeda, string pais)
    {
        DefinirDados(ddi, siglaIso, moeda, simboloMoeda, pais);
    }

    public Paises(int id, string ddi, string siglaIso, string moeda, string simboloMoeda, string pais)
        : this(ddi, siglaIso, moeda, simboloMoeda, pais)
    {
        Id = id;
    }

    public void Atualizar(string ddi, string siglaIso, string moeda, string simboloMoeda, string pais)
    {
        DefinirDados(ddi, siglaIso, moeda, simboloMoeda, pais);
    }

    private void DefinirDados(
        string ddi,
        string siglaIso,
        string moeda,
        string simboloMoeda,
        string pais)
    {
        ddi = TextNormalization.Normalize(ddi);
        siglaIso = TextNormalization.Normalize(siglaIso);
        moeda = TextNormalization.Normalize(moeda);
        simboloMoeda = TextNormalization.Normalize(simboloMoeda);
        pais = TextNormalization.Normalize(pais);

        if (string.IsNullOrWhiteSpace(ddi))
            throw new DomainException("DDI é obrigatório.");

        if (string.IsNullOrWhiteSpace(siglaIso))
            throw new DomainException("Sigla ISO é obrigatória.");

        if (string.IsNullOrWhiteSpace(moeda))
            throw new DomainException("Moeda é obrigatória.");

        if (string.IsNullOrWhiteSpace(simboloMoeda))
            throw new DomainException("Símbolo da moeda é obrigatório.");

        if (string.IsNullOrWhiteSpace(pais))
            throw new DomainException("País é obrigatório.");

        Ddi = ddi;
        SiglaIso = siglaIso;
        Moeda = moeda;
        SimboloMoeda = simboloMoeda;
        Pais = pais;
    }
}
