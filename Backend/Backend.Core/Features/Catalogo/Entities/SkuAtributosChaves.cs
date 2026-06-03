using Backend.Core.Common.Exceptions;
using Backend.Core.Common.Helpers;

namespace Backend.Core.Features.Catalogo.Entities;

public class SkuAtributosChaves
{
    private readonly List<SkuAtributosValores> _skuAtributosValores = new();

    public int Id { get; private set; }
    public string Chave { get; private set; }
    public IReadOnlyCollection<SkuAtributosValores> SkuAtributosValores => _skuAtributosValores.AsReadOnly();

    protected SkuAtributosChaves() { }

    public SkuAtributosChaves(string chave)
    {
        chave = TextNormalization.Normalize(chave);

        if (string.IsNullOrWhiteSpace(chave))
            throw new DomainException("Chave de atributo é obrigatória.");

        Chave = chave;
    }

    public SkuAtributosChaves(int id, string chave)
        : this(chave)
    {
        Id = id;
    }

    public void Atualizar(string chave)
    {
        chave = TextNormalization.Normalize(chave);

        if (string.IsNullOrWhiteSpace(chave))
            throw new DomainException("Chave de atributo é obrigatória.");

        Chave = chave;
    }

    public void AdicionarValor(SkuAtributosValores valor)
    {
        if (valor == null)
            throw new DomainException("Valor de atributo é obrigatório.");

        if (_skuAtributosValores.Any(x => x.Valor.Equals(valor.Valor, StringComparison.OrdinalIgnoreCase)))
            throw new DomainException("Valor de atributo duplicado.");

        _skuAtributosValores.Add(valor);
    }

    public void RemoverValor(SkuAtributosValores valor)
    {
        if (valor == null)
            throw new DomainException("Valor de atributo é obrigatório.");

        _skuAtributosValores.Remove(valor);
    }

    public void LimparValores()
    {
        _skuAtributosValores.Clear();
    }
}
