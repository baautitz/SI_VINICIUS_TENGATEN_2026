using Backend.Core.Common.Helpers;
using Backend.Core.Common.Results;
using Backend.Core.Common.ValueObjects;

namespace Backend.Core.Features.Localizacao.Entities;

public class Cidades
{
    public int Id { get; private set; }
    public string Cidade { get; private set; } = null!;
    public string Ddd { get; private set; } = null!;
    public Estados Estado { get; private set; } = null!;

    private Cidades(string cidade, string ddd, Estados estado)
    {
        Cidade = cidade;
        Ddd = ddd;
        Estado = estado;
    }


    protected Cidades() { }

    public Cidades(int id, string cidade, string ddd, Estados estado)
        : this(cidade, ddd, estado)
    {
        Id = id;
    }

    public static Resultado<Cidades> Criar(string cidade, string ddd, Estados estado)
    {
        var validation = ValidarDados(cidade, ddd, estado, out var normalizedCidade, out var normalizedEstado, out var normalizedDdd);
        if (!validation.Success)
            return Resultado<Cidades>.Falha(validation.Errors!);

        return Resultado<Cidades>.Sucesso(new Cidades(normalizedCidade, normalizedDdd, normalizedEstado!));
    }

    public Resultado<Cidades> AtualizarResultado(string cidade, string ddd, Estados estado)
    {
        var validation = ValidarDados(cidade, ddd, estado, out var normalizedCidade, out var normalizedEstado, out var normalizedDdd);
        if (!validation.Success)
            return Resultado<Cidades>.Falha(validation.Errors!);

        Cidade = normalizedCidade;
        Ddd = normalizedDdd;
        Estado = normalizedEstado!;

        return Resultado<Cidades>.Sucesso(this);
    }

    private static Resultado ValidarDados(string cidade, string ddd, Estados estado, out string normalizedCidade, out Estados? normalizedEstado, out string normalizedDdd)
    {
        normalizedCidade = TextNormalization.Normalize(cidade);
        normalizedEstado = estado;
        normalizedDdd = new Ddd(ddd).Valor;

        if (string.IsNullOrWhiteSpace(normalizedCidade))
            return Resultado.Falha(new ResultadoErro("CIDADE_OBRIGATORIA", "Cidade é obrigatória.", "Cidade"));

        if (string.IsNullOrWhiteSpace(normalizedDdd))
            return Resultado.Falha(new ResultadoErro("DDD_OBRIGATORIO", "DDD é obrigatório.", "Ddd"));

        if (normalizedDdd.Length < 2 || normalizedDdd.Length > 4)
            return Resultado.Falha(new ResultadoErro("DDD_INVALIDO", "DDD deve conter entre 2 e 4 números.", "Ddd"));

        if (normalizedEstado is null)
            return Resultado.Falha(new ResultadoErro("ESTADO_OBRIGATORIO", "Estado é obrigatório para cidade.", "Estado"));

        return Resultado.Sucesso();
    }
}
