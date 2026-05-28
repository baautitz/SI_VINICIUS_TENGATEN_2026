using Backend.Core.Common;
using Backend.Core.Features.Localizacao.Entities;

namespace Backend.Core.Features.Localizacao.Entities;

public class Estados
{
    public int Id { get; private set; }
    public string Estado { get; private set; }
    public string Uf { get; private set; }
    public Paises Pais { get; private set; }

    public Estados(string estado, string uf, Paises pais)
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

    public Estados(int id, string estado, string uf, Paises pais)
        : this(estado, uf, pais)
    {
        Id = id;
    }

    public void Atualizar(string estado, string uf, Paises pais)
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

    public void DefinirPais(Paises pais)
    {
        Pais = pais ?? throw new DomainException("País é obrigatório para estado.");
    }
}
