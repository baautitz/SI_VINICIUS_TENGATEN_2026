using Backend.Core.Common;
using Backend.Core.Features.Localizacao.Entities;

namespace Backend.Core.Features.Localizacao.Entities;

public class Cidades
{
    public int Id { get; private set; }
    public string Cidade { get; private set; }
    public short Ddd { get; private set; }
    public Estados Estado { get; private set; }

    public Cidades(string cidade, short ddd, Estados estado)
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

    public Cidades(int id, string cidade, short ddd, Estados estado)
        : this(cidade, ddd, estado)
    {
        Id = id;
    }

    public void Atualizar(string cidade, short ddd, Estados estado)
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

    public void DefinirEstado(Estados estado)
    {
        Estado = estado ?? throw new DomainException("Estado é obrigatório para cidade.");
    }
}
