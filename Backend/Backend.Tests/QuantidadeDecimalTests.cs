using Backend.Core.Common.Exceptions;
using Backend.Core.Features.Catalogo.Entities;
using Backend.Core.Features.Estoque.Entities;
using Backend.Core.Features.Estoque.Entities.Enums;

namespace Backend.Tests;

// Unidade sem "permite decimais" só aceita quantidade inteira em qualquer movimentação de estoque.
public class QuantidadeDecimalTests
{
    private static Skus Sku(bool permiteDecimais)
    {
        var un = new UnidadesMedida(permiteDecimais ? "KG" : "UN", "Unidade", "CONTAGEM", permiteDecimais);
        var produto = new Produtos("Produto", null, new Categorias("Cat"), new Marcas("Marca"), un);
        var sku = new Skus("SKU-1", 10m);
        sku.AtualizarProduto(produto);
        return sku;
    }

    private static MovimentacoesEstoques Movimentacao()
        => new(TipoMovimentacaoEstoque.ENTRADA, OrigemMovimentacaoEstoque.MANUAL, null, "Motivo de teste", null, null);

    [Theory]
    [InlineData(false, 2, true)]
    [InlineData(false, 1.5, false)]
    [InlineData(true, 1.5, true)]
    [InlineData(true, 2, true)]
    public void AceitaQuantidade_segue_permite_decimais(bool permiteDecimais, double quantidade, bool esperado)
        => Assert.Equal(esperado, Sku(permiteDecimais).AceitaQuantidade((decimal)quantidade));

    [Fact]
    public void Movimentacao_rejeita_fracao_em_unidade_inteira()
    {
        var ex = Assert.Throws<DomainException>(() => Movimentacao().AdicionarItem(Sku(false), 1.5m, 1m));
        Assert.Contains("UN", ex.Message);
    }

    [Fact]
    public void Movimentacao_aceita_fracao_em_unidade_com_decimais()
    {
        var m = Movimentacao();
        m.AdicionarItem(Sku(true), 1.5m, 1m);
        Assert.Single(m.MovimentacoesEstoquesItens);
    }
}
