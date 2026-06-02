using Backend.Core.Common.Exceptions;
using Backend.Core.Common.Helpers;
using Backend.Core.Features.UnidadeMedida.Entities;

namespace Backend.Core.Features.Catalogo.Entities;

public class Produtos
{
    private readonly List<Skus> _skus = new();

    public int Id { get; private set; }
    public string Produto { get; private set; }
    public string Descricao { get; private set; }
    public bool Ativo { get; private set; }
    public Categorias Categoria { get; private set; }
    public Marcas Marca { get; private set; }
    public UnidadesMedida UnidadeMedida { get; private set; }
    public IReadOnlyCollection<Skus> Skus => _skus.AsReadOnly();

    public Produtos(string produto, string descricao, Categorias categoria, Marcas marca, UnidadesMedida unidadeMedida)
    {
        produto = TextNormalization.Normalize(produto);
        descricao = TextNormalization.Normalize(descricao);

        if (string.IsNullOrWhiteSpace(produto))
            throw new DomainException("Nome do produto é obrigatório.");

        if (string.IsNullOrWhiteSpace(descricao))
            throw new DomainException("Descrição do produto é obrigatória.");

        Categoria = categoria ?? throw new DomainException("Categoria é obrigatória.");
        Marca = marca ?? throw new DomainException("Marca é obrigatória.");
        UnidadeMedida = unidadeMedida ?? throw new DomainException("Unidade de medida é obrigatória.");

        Produto = produto;
        Descricao = descricao;
        Ativo = true;
    }

    public Produtos(int id, string produto, string descricao, Categorias categoria, Marcas marca, UnidadesMedida unidadeMedida)
        : this(produto, descricao, categoria, marca, unidadeMedida)
    {
        Id = id;
    }

    public void Atualizar(string produto, string descricao, Categorias categoria, Marcas marca, UnidadesMedida unidadeMedida)
    {
        produto = TextNormalization.Normalize(produto);
        descricao = TextNormalization.Normalize(descricao);

        if (string.IsNullOrWhiteSpace(produto))
            throw new DomainException("Nome do produto é obrigatório.");

        if (string.IsNullOrWhiteSpace(descricao))
            throw new DomainException("Descrição do produto é obrigatória.");

        Categoria = categoria ?? throw new DomainException("Categoria é obrigatória.");
        Marca = marca ?? throw new DomainException("Marca é obrigatória.");
        UnidadeMedida = unidadeMedida ?? throw new DomainException("Unidade de medida é obrigatória.");

        Produto = produto;
        Descricao = descricao;
    }

    public void Ativar() => Ativo = true;

    public void Desativar() => Ativo = false;

    public void AdicionarSku(Skus sku)
    {
        if (sku == null)
            throw new DomainException("SKU é obrigatório.");

        if (_skus.Any(x => x.Sku == sku.Sku))
            throw new DomainException("Já existe um SKU com o mesmo identificador.");

        _skus.Add(sku);
    }

    public void RemoverSku(Skus sku)
    {
        if (sku == null)
            throw new DomainException("SKU é obrigatório.");

        _skus.Remove(sku);
    }
}
