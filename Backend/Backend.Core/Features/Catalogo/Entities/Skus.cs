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
    public decimal CustoMedio { get; private set; }
    public decimal CustoUltimaCompra { get; private set; }
    public bool Ativo { get; private set; }
    public Produtos? Produto { get; private set; }

    public string NomeExibicao => Produto != null
        ? (_atributos.Any()
            ? $"{Produto.Produto} - {string.Join(" / ", _atributos.Select(a => a.Valor))}"
            : Produto.Produto)
        : Sku;

    public IReadOnlyCollection<SkuAtributosValores> Atributos => _atributos.AsReadOnly();

    protected Skus() { }

    public Skus(string sku, decimal preco, decimal estoque = 0, string? gtinEan = null, decimal custoMedio = 0, decimal custoUltimaCompra = 0, Produtos? produto = null)
    {
        sku = TextNormalization.Normalize(sku);
        var gtinEanVo = string.IsNullOrWhiteSpace(gtinEan) ? null : new DocumentoGenerico(gtinEan);

        if (string.IsNullOrWhiteSpace(sku))
            throw new DomainException("SKU é obrigatório.");

        if (preco < 0)
            throw new DomainException("Preço não pode ser negativo.");

        if (estoque < 0)
            throw new DomainException("Estoque não pode ser negativo.");

        if (custoMedio < 0)
            throw new DomainException("Custo médio não pode ser negativo.");

        if (custoUltimaCompra < 0)
            throw new DomainException("Custo de última compra não pode ser negativo.");

        Sku = sku;
        Preco = preco;
        Estoque = estoque;
        GtinEan = gtinEanVo?.Valor;
        CustoMedio = custoMedio;
        CustoUltimaCompra = custoUltimaCompra;
        Ativo = true;
        
        Produto = produto;
    }

    public Skus(string sku, decimal preco, decimal estoque, bool ativo, string? gtinEan = null, decimal custoMedio = 0, decimal custoUltimaCompra = 0, Produtos? produto = null)
        : this(sku, preco, estoque, gtinEan, custoMedio, custoUltimaCompra, produto)
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

    public void RegistrarEntradaDeEstoque(decimal quantidadeEntrada, decimal custoUnitarioPago)
    {
        if (quantidadeEntrada <= 0)
            throw new DomainException("A quantidade de entrada deve ser maior que zero.");

        if (custoUnitarioPago < 0)
            throw new DomainException("O custo unitário não pode ser negativo.");

        // NovoCustoMedio = ((Estoque Atual * Custo Medio Atual) + (Qtd Entrada * Custo Pago)) / (Estoque Atual + Qtd Entrada)
        decimal valorEstoqueAtual = Estoque * CustoMedio;
        decimal valorEntrada = quantidadeEntrada * custoUnitarioPago;
        decimal novoEstoqueTotal = Estoque + quantidadeEntrada;

        CustoMedio = novoEstoqueTotal == 0 ? 0 : Math.Round((valorEstoqueAtual + valorEntrada) / novoEstoqueTotal, 4);
        CustoUltimaCompra = custoUnitarioPago;
        Estoque = novoEstoqueTotal;
    }

    public void ReverterEntradaDeEstoque(decimal quantidadeCancelada, decimal custoMedioAnterior)
    {
        if (Estoque - quantidadeCancelada < 0)
            throw new DomainException("Estoque não pode ficar negativo ao reverter a entrada.");

        Estoque -= quantidadeCancelada;
        CustoMedio = custoMedioAnterior;
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
