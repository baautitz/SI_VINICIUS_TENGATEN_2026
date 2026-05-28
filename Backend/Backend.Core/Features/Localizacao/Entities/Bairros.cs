using Backend.Core.Common;
using Backend.Core.Features.Localizacao.Entities;

namespace Backend.Core.Features.Localizacao.Entities;

public class Bairros
{
    public int Id { get; private set; }
    public string Bairro { get; private set; }
    public Cidades Cidade { get; private set; }

    public Bairros(string bairro, Cidades cidade)
    {
        bairro = TextNormalization.Normalize(bairro);

        if (string.IsNullOrWhiteSpace(bairro))
            throw new DomainException("Bairro é obrigatório.");

        Cidade = cidade ?? throw new DomainException("Cidade é obrigatória para bairro.");

        Bairro = bairro;
    }

    public Bairros(int id, string bairro, Cidades cidade)
        : this(bairro, cidade)
    {
        Id = id;
    }

    public void Atualizar(string bairro, Cidades cidade)
    {
        bairro = TextNormalization.Normalize(bairro);

        if (string.IsNullOrWhiteSpace(bairro))
            throw new DomainException("Bairro é obrigatório.");

        Cidade = cidade ?? throw new DomainException("Cidade é obrigatória para bairro.");

        Bairro = bairro;
    }

    public void DefinirCidade(Cidades cidade)
    {
        Cidade = cidade ?? throw new DomainException("Cidade é obrigatória para bairro.");
    }
}
