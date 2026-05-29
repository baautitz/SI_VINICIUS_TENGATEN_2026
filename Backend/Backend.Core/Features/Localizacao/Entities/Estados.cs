using Backend.Core.Common;
using Backend.Core.Features.Localizacao.Entities;

namespace Backend.Core.Features.Localizacao.Entities;

public class Estados
{
    public int Id { get; private set; }
    public string Estado { get; private set; } = null!;
    public string Uf { get; private set; } = null!;
    public Paises Pais { get; private set; } = null!;

    public Estados(string estado, string uf, Paises pais)
    {
        DefinirDados(estado, uf, pais);
    }

    public Estados(int id, string estado, string uf, Paises pais)
        : this(estado, uf, pais)
    {
        Id = id;
    }

    public static Resultado<Estados> Criar(string estado, string uf, Paises pais)
    {
        try
        {
            return Resultado<Estados>.Sucesso(new Estados(estado, uf, pais));
        }
        catch (DomainException ex)
        {
            return Resultado<Estados>.Falha(new ResultadoErro("ESTADO_INVALIDO", ex.Message));
        }
    }

    public Resultado<Estados> AtualizarResultado(string estado, string uf, Paises pais)
    {
        try
        {
            Atualizar(estado, uf, pais);
            return Resultado<Estados>.Sucesso(this);
        }
        catch (DomainException ex)
        {
            return Resultado<Estados>.Falha(new ResultadoErro("ESTADO_INVALIDO", ex.Message));
        }
    }

    public void Atualizar(string estado, string uf, Paises pais)
    {
        DefinirDados(estado, uf, pais);
    }

    public void DefinirPais(Paises pais)
    {
        Pais = pais ?? throw new DomainException("País é obrigatório para estado.");
    }

    private void DefinirDados(string estado, string uf, Paises pais)
    {
        estado = TextNormalization.Normalize(estado);
        uf = TextNormalization.Normalize(uf);

        if (string.IsNullOrWhiteSpace(estado))
            throw new DomainException("Estado é obrigatório.");

        if (string.IsNullOrWhiteSpace(uf))
            throw new DomainException("UF é obrigatório.");

        Pais = pais ?? throw new DomainException("País é obrigatório para estado.");

        Estado = estado;
        Uf = uf;
    }
}
