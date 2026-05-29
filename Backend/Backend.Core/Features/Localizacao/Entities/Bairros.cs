using Backend.Core.Common;
using Backend.Core.Features.Localizacao.Entities;

namespace Backend.Core.Features.Localizacao.Entities;

public class Bairros
{
    public int Id { get; private set; }
    public string Bairro { get; private set; } = null!;
    public Cidades Cidade { get; private set; } = null!;

    public Bairros(string bairro, Cidades cidade)
    {
        DefinirDados(bairro, cidade);
    }

    public Bairros(int id, string bairro, Cidades cidade)
        : this(bairro, cidade)
    {
        Id = id;
    }

    public static Resultado<Bairros> Criar(string bairro, Cidades cidade)
    {
        try
        {
            return Resultado<Bairros>.Sucesso(new Bairros(bairro, cidade));
        }
        catch (DomainException ex)
        {
            return Resultado<Bairros>.Falha(new ResultadoErro("BAIRRO_INVALIDO", ex.Message));
        }
    }

    public Resultado<Bairros> AtualizarResultado(string bairro, Cidades cidade)
    {
        try
        {
            Atualizar(bairro, cidade);
            return Resultado<Bairros>.Sucesso(this);
        }
        catch (DomainException ex)
        {
            return Resultado<Bairros>.Falha(new ResultadoErro("BAIRRO_INVALIDO", ex.Message));
        }
    }

    public void Atualizar(string bairro, Cidades cidade)
    {
        DefinirDados(bairro, cidade);
    }

    public void DefinirCidade(Cidades cidade)
    {
        Cidade = cidade ?? throw new DomainException("Cidade é obrigatória para bairro.");
    }

    private void DefinirDados(string bairro, Cidades cidade)
    {
        bairro = TextNormalization.Normalize(bairro);

        if (string.IsNullOrWhiteSpace(bairro))
            throw new DomainException("Bairro é obrigatório.");

        Cidade = cidade ?? throw new DomainException("Cidade é obrigatória para bairro.");

        Bairro = bairro;
    }
}
