using Backend.Core.Common;

namespace Backend.Core.Features.Catalogo.Entities;

public class SkuAtributosChaves
{
    private readonly List<SkusAtributosValores> _skusAtributosValores = new();

    public int Id { get; private set; }
    public string Chave { get; private set; }
    public IReadOnlyCollection<SkusAtributosValores> SkusAtributosValores => _skusAtributosValores.AsReadOnly();

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

    public void AdicionarValor(SkusAtributosValores valor)
    {
        if (valor == null)
            throw new DomainException("Valor de atributo é obrigatório.");

        if (_skusAtributosValores.Any(x => x.Valor == valor.Valor && x.ChaveId == valor.ChaveId))
            throw new DomainException("Valor de atributo duplicado.");

        _skusAtributosValores.Add(valor);
    }

    public void RemoverValor(SkusAtributosValores valor)
    {
        if (valor == null)
            throw new DomainException("Valor de atributo é obrigatório.");

        _skusAtributosValores.Remove(valor);
    }
}
