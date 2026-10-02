using Backend.Core.Common.Exceptions;
using Backend.Core.Features.Catalogo.Entities;
using Backend.Core.Features.Compras.Services;

namespace Backend.Tests;

public class RateioCompraTests
{
    [Fact]
    public void Cem_em_tres_itens_iguais_fecha_com_residuo_no_maior()
    {
        var r = RateioCompra.Ratear(new[] { 10m, 10m, 10m }, 100m);

        Assert.Equal(100m, r.Sum());
        Assert.Equal(new[] { 33.33m, 33.33m, 33.34m }, r.OrderBy(x => x));
    }

    [Fact]
    public void Soma_do_rateio_sempre_fecha_exata_e_custo_bate_com_tolerancia()
    {
        var qtds = new[] { 3m, 7m, 1.5m, 11m };
        var valores = new[] { 17.77m, 3.01m, 120.5m, 0.99m };
        var despesas = 53.37m - 4.11m; // frete+seguro+outras+frete terceiro - desconto

        var r = RateioCompra.Ratear(valores, despesas);

        Assert.Equal(despesas, r.Sum());
        var totalCusto = valores.Select((v, i) => RateioCompra.CustoUnitario(qtds[i], v, r[i]) * qtds[i]).Sum();
        // custo unitário tem 4 casas: erro máximo de 0,00005 por unidade
        Assert.InRange(totalCusto - (valores.Sum() + despesas), -0.001m, 0.001m);
    }

    [Fact]
    public void Residuo_vai_para_o_maior_item_mesmo_com_ultimo_zerado()
    {
        var r = RateioCompra.Ratear(new[] { 10m, 20m, 0m }, 10m);

        Assert.Equal(10m, r.Sum());
        Assert.Equal(0m, r[2]);
    }

    [Fact]
    public void Rejeita_total_de_produtos_zero()
        => Assert.Throws<DomainException>(() => RateioCompra.Ratear(new[] { 0m }, 5m));

    [Fact]
    public void Rejeita_desconto_que_deixa_custo_negativo()
        => Assert.Throws<DomainException>(() => RateioCompra.Ratear(new[] { 10m, 10m }, -30m));

    // C1: estoque valorizado (estoque x custo médio) bate com o valor pago, mesmo em quantidades grandes.
    [Theory]
    [InlineData(7000, 100.00)]
    [InlineData(30000, 1.00)]
    [InlineData(1234567, 9999.99)]
    public void Entrada_valoriza_o_estoque_exatamente_pelo_valor_pago(double qtd, double pago)
    {
        var sku = new Skus("SKU-1", 10m);
        var custo = RateioCompra.CustoUnitario((decimal)qtd, (decimal)pago, 0m);

        sku.RegistrarEntradaDeEstoque((decimal)qtd, custo);

        Assert.Equal((decimal)pago, Math.Round(sku.Estoque * sku.CustoMedio, 2, MidpointRounding.AwayFromZero));
    }
}
