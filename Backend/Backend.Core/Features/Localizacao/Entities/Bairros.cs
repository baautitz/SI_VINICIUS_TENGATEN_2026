using Backend.Core.Common;

namespace Backend.Core.Features.Localizacao.Entities;

public class Bairros
{
    public int Id { get; private set; }
    public string Bairro { get; private set; } = null!;
    public Cidades Cidade { get; private set; } = null!;

    private Bairros(string bairro, Cidades cidade)
    {
        Bairro = bairro;
        Cidade = cidade;
    }

    public Bairros(int id, string bairro, Cidades cidade)
        : this(bairro, cidade)
    {
        Id = id;
    }

    public static Resultado<Bairros> Criar(string bairro, Cidades cidade)
    {
        var validation = ValidarDados(bairro, cidade, out var normalizedBairro, out var normalizedCidade);
        if (!validation.Success)
            return Resultado<Bairros>.Falha(validation.Errors!);

        return Resultado<Bairros>.Sucesso(new Bairros(normalizedBairro, normalizedCidade!));
    }

    public Resultado<Bairros> AtualizarResultado(string bairro, Cidades cidade)
    {
        var validation = ValidarDados(bairro, cidade, out var normalizedBairro, out var normalizedCidade);
        if (!validation.Success)
            return Resultado<Bairros>.Falha(validation.Errors!);

        Bairro = normalizedBairro;
        Cidade = normalizedCidade!;

        return Resultado<Bairros>.Sucesso(this);
    }

    private static Resultado ValidarDados(string bairro, Cidades cidade, out string normalizedBairro, out Cidades? normalizedCidade)
    {
        normalizedBairro = TextNormalization.Normalize(bairro);
        normalizedCidade = cidade;

        if (string.IsNullOrWhiteSpace(normalizedBairro))
            return Resultado.Falha(new ResultadoErro("BAIRRO_OBRIGATORIO", "Bairro é obrigatório.", "Bairro"));

        if (normalizedCidade is null)
            return Resultado.Falha(new ResultadoErro("CIDADE_OBRIGATORIA", "Cidade é obrigatória para bairro.", "Cidade"));

        return Resultado.Sucesso();
    }
}
