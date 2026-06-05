using Backend.Core.Common.Exceptions;
using Backend.Core.Common.Helpers;
namespace Backend.Core.Features.Catalogo.Entities;

public class Marcas
{
    public int Id { get; private set; }
    public string Marca { get; private set; } = null!;
    public string? Descricao { get; private set; }
    public bool Ativo { get; private set; }

    protected Marcas() { }

    public Marcas(string marca, string? descricao = null)
    {
        marca = TextNormalization.Normalize(marca);
        descricao = TextNormalization.NormalizeOrNull(descricao);

        if (string.IsNullOrWhiteSpace(marca))
            throw new DomainException("Marca é obrigatória.");

        Marca = marca;
        Descricao = descricao;
        Ativo = true;
    }

    public Marcas(int id, string marca, string? descricao = null)
        : this(marca, descricao)
    {
        Id = id;
    }

    public void Atualizar(string marca, string? descricao = null)
    {
        marca = TextNormalization.Normalize(marca);
        descricao = TextNormalization.NormalizeOrNull(descricao);

        if (string.IsNullOrWhiteSpace(marca))
            throw new DomainException("Marca é obrigatória.");

        Marca = marca;
        Descricao = descricao;
    }

    public void Ativar() => Ativo = true;

    public void Desativar() => Ativo = false;
}
