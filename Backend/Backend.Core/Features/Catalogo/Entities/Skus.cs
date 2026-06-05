using Backend.Core.Common.Exceptions;
using Backend.Core.Common.Helpers;
using Backend.Core.Common.ValueObjects;

namespace Backend.Core.Features.Catalogo.Entities;

public class Skus
{
    private readonly List<SkuAtributosValores> _atributos = new();

    public string Sku { get; private set; } = null!;
    public string? GtinEan { get; private set; }
    public decimal Preco { get; private set; }
    public decimal Estoque { get; private set; }
    public bool Ativo { get; private set; }
    public IReadOnlyCollection<SkuAtributosValores> SkuAtributosValores => _atributos.AsReadOnly();

    protected Skus() { }

    public Skus(string sku, decimal preco, decimal estoque = 0, string? gtinEan = null)
    {
        sku = TextNormalization.Normalize(sku);
        var gtinEanVo = string.IsNullOrWhiteSpace(gtinEan) ? null : new DocumentoGenerico(gtinEan);

        if (string.IsNullOrWhiteSpace(sku))
            throw new DomainException("SKU é obrigatório.");

        if (preco < 0)
            throw new DomainException("Preço não pode ser negativo.");

        if (estoque < 0)
            throw new DomainException("Estoque não pode ser negativo.");

        Sku = sku;
        Preco = preco;
        Estoque = estoque;
        GtinEan = gtinEanVo?.Valor;
        Ativo = true;
    }

    public Skus(string sku, decimal preco, decimal estoque, bool ativo, string? gtinEan = null)
        : this(sku, preco, estoque, gtinEan)
    {
        if (!ativo)
            Desativar();
    }

    public void AtualizarPreco(decimal preco)
    {
        if (preco < 0)
            throw new DomainException("Preço não pode ser negativo.");

        Preco = preco;
    }

    public void AjustarEstoque(decimal quantidade)
    {
        if (Estoque + quantidade < 0)
            throw new DomainException("Estoque não pode ficar negativo.");

        Estoque += quantidade;
    }

    public void Ativar() => Ativo = true;

    public void Desativar() => Ativo = false;

    public void AdicionarAtributo(SkuAtributosValores atributo)
    {
        if (atributo == null)
            throw new DomainException("Atributo é obrigatório.");

        if (_atributos.Any(x => x.ChaveId == atributo.ChaveId && x.Valor.Equals(atributo.Valor, StringComparison.OrdinalIgnoreCase)))
            throw new DomainException("Atributo duplicado para este SKU.");

        _atributos.Add(atributo);
    }

    public void RemoverAtributo(SkuAtributosValores atributo)
    {
        if (atributo == null)
            throw new DomainException("Atributo é obrigatório.");

        _atributos.Remove(atributo);
    }

    public void DefinirAtributos(IEnumerable<SkuAtributosValores> atributos)
    {
        if (atributos == null)
            throw new DomainException("Atributos são obrigatórios.");

        _atributos.Clear();
        _atributos.AddRange(atributos);
    }
}
