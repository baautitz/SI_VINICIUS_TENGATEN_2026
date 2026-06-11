using Backend.Core.Common.Helpers;
using Backend.Core.Common.Results;
using Backend.Core.Common.ValueObjects;

namespace Backend.Core.Features.Localizacao.Entities;

public class Paises
{
    public int Id { get; private set; }
    public Ddi Ddi { get; private set; }
    public string CodigoIsoPais { get; private set; } = null!;
    public string CodigoIsoMoeda { get; private set; } = null!;
    public string SimboloMoeda { get; private set; } = null!;
    public string Pais { get; private set; } = null!;

    private Paises(Ddi ddi, string codigoIsoPais, string codigoIsoMoeda, string simboloMoeda, string pais)
    {
        Ddi = ddi;
        CodigoIsoPais = codigoIsoPais;
        CodigoIsoMoeda = codigoIsoMoeda;
        SimboloMoeda = simboloMoeda;
        Pais = pais;
    }


    protected Paises() { }

    public Paises(int id, Ddi ddi, string codigoIsoPais, string codigoIsoMoeda, string simboloMoeda, string pais)
        : this(ddi, codigoIsoPais, codigoIsoMoeda, simboloMoeda, pais)
    {
        Id = id;
    }

    public static Resultado<Paises> Criar(Ddi ddi, string codigoIsoPais, string codigoIsoMoeda, string simboloMoeda, string pais)
    {
        var validation = ValidarDados(ddi, codigoIsoPais, codigoIsoMoeda, simboloMoeda, pais,
            out var normalizedDdi,
            out var normalizedCodigoIsoPais,
            out var normalizedCodigoIsoMoeda,
            out var normalizedSimboloMoeda,
            out var normalizedPais);

        if (!validation.Success)
            return Resultado<Paises>.Falha(validation.Errors!);

        return Resultado<Paises>.Sucesso(new Paises(normalizedDdi, normalizedCodigoIsoPais, normalizedCodigoIsoMoeda, normalizedSimboloMoeda, normalizedPais));
    }

    public Resultado<Paises> AtualizarResultado(Ddi ddi, string codigoIsoPais, string codigoIsoMoeda, string simboloMoeda, string pais)
    {
        var validation = ValidarDados(ddi, codigoIsoPais, codigoIsoMoeda, simboloMoeda, pais,
            out var normalizedDdi,
            out var normalizedCodigoIsoPais,
            out var normalizedCodigoIsoMoeda,
            out var normalizedSimboloMoeda,
            out var normalizedPais);

        if (!validation.Success)
            return Resultado<Paises>.Falha(validation.Errors!);

        Ddi = normalizedDdi;
        CodigoIsoPais = normalizedCodigoIsoPais;
        CodigoIsoMoeda = normalizedCodigoIsoMoeda;
        SimboloMoeda = normalizedSimboloMoeda;
        Pais = normalizedPais;

        return Resultado<Paises>.Sucesso(this);
    }

    private static Resultado ValidarDados(
        Ddi ddi,
        string codigoIsoPais,
        string codigoIsoMoeda,
        string simboloMoeda,
        string pais,
        out Ddi normalizedDdi,
        out string normalizedCodigoIsoPais,
        out string normalizedCodigoIsoMoeda,
        out string normalizedSimboloMoeda,
        out string normalizedPais)
    {
        normalizedDdi = new Ddi(ddi.Valor);
        normalizedCodigoIsoPais = TextNormalization.Normalize(codigoIsoPais);
        normalizedCodigoIsoMoeda = TextNormalization.Normalize(codigoIsoMoeda);
        normalizedSimboloMoeda = TextNormalization.Normalize(simboloMoeda);
        normalizedPais = TextNormalization.Normalize(pais);

        if (string.IsNullOrWhiteSpace(normalizedDdi.Valor))
            return Resultado.Falha(new ResultadoErro("DDI_OBRIGATORIO", "DDI é obrigatório.", "Ddi"));

        if (string.IsNullOrWhiteSpace(normalizedCodigoIsoPais))
            return Resultado.Falha(new ResultadoErro("CODIGO_ISO_PAIS_OBRIGATORIO", "Código ISO do país é obrigatório.", "CodigoIsoPais"));

        if (string.IsNullOrWhiteSpace(normalizedCodigoIsoMoeda))
            return Resultado.Falha(new ResultadoErro("CODIGO_ISO_MOEDA_OBRIGATORIO", "Código ISO da moeda é obrigatório.", "CodigoIsoMoeda"));

        if (string.IsNullOrWhiteSpace(normalizedSimboloMoeda))
            return Resultado.Falha(new ResultadoErro("SIMBOLO_MOEDA_OBRIGATORIO", "Símbolo da moeda é obrigatório.", "SimboloMoeda"));

        if (string.IsNullOrWhiteSpace(normalizedPais))
            return Resultado.Falha(new ResultadoErro("PAIS_OBRIGATORIO", "País é obrigatório.", "Pais"));

        return Resultado.Sucesso();
    }
}
