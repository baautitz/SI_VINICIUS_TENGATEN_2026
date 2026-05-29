using Backend.Core.Common;

namespace Backend.Core.Features.Localizacao.Entities;

public class Estados
{
    public int Id { get; private set; }
    public string Estado { get; private set; } = null!;
    public string Uf { get; private set; } = null!;
    public Paises Pais { get; private set; } = null!;

    private Estados(string estado, string uf, Paises pais)
    {
        Estado = estado;
        Uf = uf;
        Pais = pais;
    }

    public Estados(int id, string estado, string uf, Paises pais)
        : this(estado, uf, pais)
    {
        Id = id;
    }

    public static Resultado<Estados> Criar(string estado, string uf, Paises pais)
    {
        var validation = ValidarDados(estado, uf, pais, out var normalizedEstado, out var normalizedUf, out var normalizedPais);
        if (!validation.Success)
            return Resultado<Estados>.Falha(validation.Errors!);

        return Resultado<Estados>.Sucesso(new Estados(normalizedEstado, normalizedUf, normalizedPais!));
    }

    public Resultado<Estados> AtualizarResultado(string estado, string uf, Paises pais)
    {
        var validation = ValidarDados(estado, uf, pais, out var normalizedEstado, out var normalizedUf, out var normalizedPais);
        if (!validation.Success)
            return Resultado<Estados>.Falha(validation.Errors!);

        Estado = normalizedEstado;
        Uf = normalizedUf;
        Pais = normalizedPais!;

        return Resultado<Estados>.Sucesso(this);
    }

    private static Resultado ValidarDados(string estado, string uf, Paises pais, out string normalizedEstado, out string normalizedUf, out Paises? normalizedPais)
    {
        normalizedEstado = TextNormalization.Normalize(estado);
        normalizedUf = TextNormalization.Normalize(uf);
        normalizedPais = pais;

        if (string.IsNullOrWhiteSpace(normalizedEstado))
            return Resultado.Falha(new ResultadoErro("ESTADO_OBRIGATORIO", "Estado é obrigatório.", "Estado"));

        if (string.IsNullOrWhiteSpace(normalizedUf))
            return Resultado.Falha(new ResultadoErro("UF_OBRIGATORIO", "UF é obrigatório.", "Uf"));

        if (normalizedPais is null)
            return Resultado.Falha(new ResultadoErro("PAIS_OBRIGATORIO", "País é obrigatório para estado.", "Pais"));

        return Resultado.Sucesso();
    }
}
