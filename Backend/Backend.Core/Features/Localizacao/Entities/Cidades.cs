using Backend.Core.Common;

namespace Backend.Core.Features.Localizacao.Entities;

public class Cidades
{
    public int Id { get; private set; }
    public string Cidade { get; private set; } = null!;
    public short Ddd { get; private set; }
    public Estados Estado { get; private set; } = null!;

    private Cidades(string cidade, short ddd, Estados estado)
    {
        Cidade = cidade;
        Ddd = ddd;
        Estado = estado;
    }

    // For Dapper ORM materialization
    protected Cidades() { }

    public Cidades(int id, string cidade, short ddd, Estados estado)
        : this(cidade, ddd, estado)
    {
        Id = id;
    }

    public static Resultado<Cidades> Criar(string cidade, short ddd, Estados estado)
    {
        var validation = ValidarDados(cidade, ddd, estado, out var normalizedCidade, out var normalizedEstado);
        if (!validation.Success)
            return Resultado<Cidades>.Falha(validation.Errors!);

        return Resultado<Cidades>.Sucesso(new Cidades(normalizedCidade, ddd, normalizedEstado!));
    }

    public Resultado<Cidades> AtualizarResultado(string cidade, short ddd, Estados estado)
    {
        var validation = ValidarDados(cidade, ddd, estado, out var normalizedCidade, out var normalizedEstado);
        if (!validation.Success)
            return Resultado<Cidades>.Falha(validation.Errors!);

        Cidade = normalizedCidade;
        Ddd = ddd;
        Estado = normalizedEstado!;

        return Resultado<Cidades>.Sucesso(this);
    }

    private static Resultado ValidarDados(string cidade, short ddd, Estados estado, out string normalizedCidade, out Estados? normalizedEstado)
    {
        normalizedCidade = TextNormalization.Normalize(cidade);
        normalizedEstado = estado;

        if (string.IsNullOrWhiteSpace(normalizedCidade))
            return Resultado.Falha(new ResultadoErro("CIDADE_OBRIGATORIA", "Cidade é obrigatória.", "Cidade"));

        if (ddd <= 0)
            return Resultado.Falha(new ResultadoErro("DDD_INVALIDO", "DDD deve ser maior que zero.", "Ddd"));

        if (normalizedEstado is null)
            return Resultado.Falha(new ResultadoErro("ESTADO_OBRIGATORIO", "Estado é obrigatório para cidade.", "Estado"));

        return Resultado.Sucesso();
    }
}
