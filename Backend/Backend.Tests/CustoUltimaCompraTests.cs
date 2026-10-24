using Backend.Core.Features.Catalogo.Entities;

namespace Backend.Tests;

// Só entrada de compra define o custo da última compra; as demais entradas movem estoque e custo médio.
public class CustoUltimaCompraTests
{
    [Fact]
    public void Entrada_de_compra_atualiza_custo_da_ultima_compra()
    {
        var sku = new Skus("SKU-1", 10m, estoque: 10, custoMedio: 5m, custoUltimaCompra: 5m);

        sku.RegistrarEntradaDeEstoque(10, 7m);

        Assert.Equal(7m, sku.CustoUltimaCompra);
        Assert.Equal(6m, sku.CustoMedio);
    }

    [Fact]
    public void Entrada_que_nao_e_compra_preserva_custo_da_ultima_compra_e_move_custo_medio()
    {
        var sku = new Skus("SKU-1", 10m, estoque: 10, custoMedio: 5m, custoUltimaCompra: 5m);

        sku.RegistrarEntradaDeEstoque(10, 7m, entradaDeCompra: false);

        Assert.Equal(5m, sku.CustoUltimaCompra);
        Assert.Equal(6m, sku.CustoMedio);
        Assert.Equal(20m, sku.Estoque);
    }

    [Fact]
    public void Entrada_ao_custo_medio_nao_altera_o_custo_medio()
    {
        var sku = new Skus("SKU-1", 10m, estoque: 10, custoMedio: 5m, custoUltimaCompra: 4m);

        sku.RegistrarEntradaDeEstoque(5, sku.CustoMedio, entradaDeCompra: false);

        Assert.Equal(5m, sku.CustoMedio);
        Assert.Equal(4m, sku.CustoUltimaCompra);
    }
}
