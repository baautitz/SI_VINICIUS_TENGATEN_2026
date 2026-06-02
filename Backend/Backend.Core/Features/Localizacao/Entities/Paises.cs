using Backend.Core.Common.Helpers;
using Backend.Core.Common.Results;
using Backend.Core.Common.ValueObjects;

namespace Backend.Core.Features.Localizacao.Entities;

public class Paises
{
    public int Id { get; private set; }
    public string Ddi { get; private set; } = null!;
    public string SiglaIso { get; private set; } = null!;
    public string Moeda { get; private set; } = null!;
    public string SimboloMoeda { get; private set; } = null!;
    public string Pais { get; private set; } = null!;

    private Paises(string ddi, string siglaIso, string moeda, string simboloMoeda, string pais)
    {
        Ddi = ddi;
        SiglaIso = siglaIso;
        Moeda = moeda;
        SimboloMoeda = simboloMoeda;
        Pais = pais;
    }

    // For Dapper ORM materialization
    protected Paises() { }

    public Paises(int id, string ddi, string siglaIso, string moeda, string simboloMoeda, string pais)
        : this(ddi, siglaIso, moeda, simboloMoeda, pais)
    {
        Id = id;
    }

    public static Resultado<Paises> Criar(string ddi, string siglaIso, string moeda, string simboloMoeda, string pais)
    {
        var validation = ValidarDados(ddi, siglaIso, moeda, simboloMoeda, pais,
            out var normalizedDdi,
            out var normalizedSiglaIso,
            out var normalizedMoeda,
            out var normalizedSimboloMoeda,
            out var normalizedPais);

        if (!validation.Success)
            return Resultado<Paises>.Falha(validation.Errors!);

        return Resultado<Paises>.Sucesso(new Paises(normalizedDdi, normalizedSiglaIso, normalizedMoeda, normalizedSimboloMoeda, normalizedPais));
    }

    public Resultado<Paises> AtualizarResultado(string ddi, string siglaIso, string moeda, string simboloMoeda, string pais)
    {
        var validation = ValidarDados(ddi, siglaIso, moeda, simboloMoeda, pais,
            out var normalizedDdi,
            out var normalizedSiglaIso,
            out var normalizedMoeda,
            out var normalizedSimboloMoeda,
            out var normalizedPais);

        if (!validation.Success)
            return Resultado<Paises>.Falha(validation.Errors!);

        Ddi = normalizedDdi;
        SiglaIso = normalizedSiglaIso;
        Moeda = normalizedMoeda;
        SimboloMoeda = normalizedSimboloMoeda;
        Pais = normalizedPais;

        return Resultado<Paises>.Sucesso(this);
    }

    private static Resultado ValidarDados(
        string ddi,
        string siglaIso,
        string moeda,
        string simboloMoeda,
        string pais,
        out string normalizedDdi,
        out string normalizedSiglaIso,
        out string normalizedMoeda,
        out string normalizedSimboloMoeda,
        out string normalizedPais)
    {
        normalizedDdi = new Ddi(ddi);
        normalizedSiglaIso = TextNormalization.Normalize(siglaIso);
        normalizedMoeda = TextNormalization.Normalize(moeda);
        normalizedSimboloMoeda = TextNormalization.Normalize(simboloMoeda);
        normalizedPais = TextNormalization.Normalize(pais);

        if (string.IsNullOrWhiteSpace(normalizedDdi))
            return Resultado.Falha(new ResultadoErro("DDI_OBRIGATORIO", "DDI é obrigatório.", "Ddi"));

        if (string.IsNullOrWhiteSpace(normalizedSiglaIso))
            return Resultado.Falha(new ResultadoErro("SIGLAISO_OBRIGATORIO", "Sigla ISO é obrigatória.", "SiglaIso"));

        if (string.IsNullOrWhiteSpace(normalizedMoeda))
            return Resultado.Falha(new ResultadoErro("MOEDA_OBRIGATORIA", "Moeda é obrigatória.", "Moeda"));

        if (string.IsNullOrWhiteSpace(normalizedSimboloMoeda))
            return Resultado.Falha(new ResultadoErro("SIMBOLO_MOEDA_OBRIGATORIO", "Símbolo da moeda é obrigatório.", "SimboloMoeda"));

        if (string.IsNullOrWhiteSpace(normalizedPais))
            return Resultado.Falha(new ResultadoErro("PAIS_OBRIGATORIO", "País é obrigatório.", "Pais"));

        return Resultado.Sucesso();
    }
}
