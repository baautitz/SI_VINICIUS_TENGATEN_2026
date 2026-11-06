using Backend.Core.Features.Acesso.Repositories;
using Backend.Core.Features.Catalogo.Entities;
using Backend.Core.Features.Catalogo.Repositories;
using Backend.Core.Features.Estoque.Entities;
using Backend.Core.Features.Estoque.Entities.Enums;
using Backend.Core.Features.Estoque.Repositories;
using Backend.Core.Features.Estoque.Services;
using Backend.Tests.Common;
using static Backend.Tests.Common.Fakes;

namespace Backend.Tests;

public class DevolucaoCompraEstoqueTests
{
    private static Skus NovoSku(decimal estoque, decimal custoMedio)
    {
        var sku = new Skus("SKU-1", 10m, estoque: estoque, custoMedio: custoMedio, custoUltimaCompra: 10m);
        var produto = Instancia<Produtos>();
        typeof(Produtos).GetProperty(nameof(Produtos.Produto))!.SetValue(produto, "Produto teste");
        var un = Instancia<UnidadesMedida>();
        typeof(UnidadesMedida).GetProperty(nameof(UnidadesMedida.Sigla))!.SetValue(un, "UN");
        produto.DefinirUnidadeMedida(un);
        sku.AtualizarProduto(produto);
        return sku;
    }

    private static MovimentacoesEstoquesService Servico(Skus sku, List<MovimentacoesEstoques> criadas)
        => new(
            Criar<IMovimentacoesEstoquesRepository>(h => h["CriarMovimentacao"] = a => { criadas.Add((MovimentacoesEstoques)a![0]!); return a[0]; }),
            Criar<ISkusRepository>(h => h["ObterSkuPorSku"] = _ => sku),
            Criar<IUsuariosRepository>(), new UowFake());

    [Fact]
    public async Task Saida_por_devolucao_de_compra_tira_a_quantidade_e_desfaz_a_contribuicao_ao_custo_medio()
    {
        // 6 un a 3,00 + compra de 4 un a 10,00 = 10 un a 5,80; devolver as 4 un da compra volta a 3,00.
        var sku = NovoSku(estoque: 10, custoMedio: 5.8m);
        var criadas = new List<MovimentacoesEstoques>();
        var saida = new MovimentacoesEstoques(TipoMovimentacaoEstoque.SAIDA, OrigemMovimentacaoEstoque.DEVOLUCAO_COMPRA, 1);
        saida.AdicionarItem(sku, 4, 10m);

        var r = await Servico(sku, criadas).Registrar(saida);

        Assert.True(r.Success);
        Assert.Equal(6, sku.Estoque);
        Assert.Equal(3m, sku.CustoMedio);
        Assert.Equal(10m, sku.CustoUltimaCompra);
        Assert.Equal(10m, Assert.Single(criadas).MovimentacoesEstoquesItens.Single().CustoUnitario);
    }

    [Fact]
    public async Task Saida_por_devolucao_de_compra_acima_do_estoque_e_recusada()
    {
        var sku = NovoSku(estoque: 3, custoMedio: 5m);
        var saida = new MovimentacoesEstoques(TipoMovimentacaoEstoque.SAIDA, OrigemMovimentacaoEstoque.DEVOLUCAO_COMPRA, 1);
        saida.AdicionarItem(sku, 4, 5m);

        var r = await Servico(sku, new()).Registrar(saida);

        Assert.Equal("ESTOQUE_INSUFICIENTE", r.Errors!.First().Code);
        Assert.Equal(3, sku.Estoque);
    }
}
