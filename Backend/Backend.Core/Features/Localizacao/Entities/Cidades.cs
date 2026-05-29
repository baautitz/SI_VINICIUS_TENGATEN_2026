using Backend.Core.Common;
using Backend.Core.Features.Localizacao.Entities;

namespace Backend.Core.Features.Localizacao.Entities;

public class Cidades
{
    public int Id { get; private set; }
    public string Cidade { get; private set; } = null!;
    public short Ddd { get; private set; }
    public Estados Estado { get; private set; } = null!;

    public Cidades(string cidade, short ddd, Estados estado)
    {
        DefinirDados(cidade, ddd, estado);
    }

    public Cidades(int id, string cidade, short ddd, Estados estado)
        : this(cidade, ddd, estado)
    {
        Id = id;
    }

    public static Resultado<Cidades> Criar(string cidade, short ddd, Estados estado)
    {
        try
        {
            return Resultado<Cidades>.Sucesso(new Cidades(cidade, ddd, estado));
        }
        catch (DomainException ex)
        {
            return Resultado<Cidades>.Falha(new ResultadoErro("CIDADE_INVALIDA", ex.Message));
        }
    }

    public Resultado<Cidades> AtualizarResultado(string cidade, short ddd, Estados estado)
    {
        try
        {
            Atualizar(cidade, ddd, estado);
            return Resultado<Cidades>.Sucesso(this);
        }
        catch (DomainException ex)
        {
            return Resultado<Cidades>.Falha(new ResultadoErro("CIDADE_INVALIDA", ex.Message));
        }
    }

    public void Atualizar(string cidade, short ddd, Estados estado)
    {
        DefinirDados(cidade, ddd, estado);
    }

    public void DefinirEstado(Estados estado)
    {
        Estado = estado ?? throw new DomainException("Estado é obrigatório para cidade.");
    }

    private void DefinirDados(string cidade, short ddd, Estados estado)
    {
        cidade = TextNormalization.Normalize(cidade);

        if (string.IsNullOrWhiteSpace(cidade))
            throw new DomainException("Cidade é obrigatória.");

        if (ddd <= 0)
            throw new DomainException("DDD deve ser maior que zero.");

        Estado = estado ?? throw new DomainException("Estado é obrigatório para cidade.");

        Cidade = cidade;
        Ddd = ddd;
    }
}
