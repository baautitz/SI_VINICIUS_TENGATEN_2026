using Backend.Core.Common.Exceptions;
using Backend.Core.Common.Helpers;
namespace Backend.Core.Features.Catalogo.Entities;

public class Categorias
{
    public int Id { get; private set; }
    public string Categoria { get; private set; }
    public string? Descricao { get; private set; }
    public bool Ativo { get; private set; }

    protected Categorias() { }

    public Categorias(string categoria, string? descricao = null)
    {
        categoria = TextNormalization.Normalize(categoria);
        descricao = TextNormalization.NormalizeOrNull(descricao);

        if (string.IsNullOrWhiteSpace(categoria))
            throw new DomainException("Categoria é obrigatória.");

        Categoria = categoria;
        Descricao = descricao;
        Ativo = true;
    }

    public Categorias(int id, string categoria, string? descricao = null)
        : this(categoria, descricao)
    {
        Id = id;
    }

    public void Atualizar(string categoria, string? descricao = null)
    {
        categoria = TextNormalization.Normalize(categoria);
        descricao = TextNormalization.NormalizeOrNull(descricao);

        if (string.IsNullOrWhiteSpace(categoria))
            throw new DomainException("Categoria é obrigatória.");

        Categoria = categoria;
        Descricao = descricao;
    }

    public void Ativar() => Ativo = true;

    public void Desativar() => Ativo = false;
}
