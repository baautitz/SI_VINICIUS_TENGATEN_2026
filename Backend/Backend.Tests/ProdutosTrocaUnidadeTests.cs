using Backend.Core.Features.Catalogo.Commands;
using Backend.Core.Features.Catalogo.Entities;
using Backend.Core.Features.Catalogo.Repositories;
using Backend.Core.Features.Catalogo.Services;
using Backend.Core.Common.Results;
using static Backend.Tests.ComprasServiceTests;

namespace Backend.Tests;

// Troca de unidade de medida: barrada quando algum SKU do produto tem movimentação de estoque, mesmo com saldo zero.
public class ProdutosTrocaUnidadeTests
{
    private static UnidadesMedida Unidade(int id, string sigla, bool decimais)
        => new(sigla, sigla, "CONTAGEM", decimais) { Id = id };

    private class Cenario
    {
        public bool Atualizou;
        public UowFake Uow = new();
        public ProdutosService Service;

        public Cenario(bool comMovimentacao)
        {
            var un = Unidade(1, "UN", false);
            var kg = Unidade(2, "KG", true);
            var produto = new Produtos(10, "Produto", null, new Categorias(1, "Cat"), new Marcas(1, "Marca"), un);
            var sku = new Skus("SKU-1", 10m, estoque: 0);

            Service = new ProdutosService(
                Criar<IProdutosRepository>(h =>
                {
                    h["ObterProdutoPorId"] = _ => produto;
                    h["ExisteProduto"] = _ => false;
                    h["AtualizarProduto"] = a => { Atualizou = true; return a![1]; };
                }),
                Criar<ICategoriasRepository>(h => h["ObterCategoriaPorId"] = _ => new Categorias(1, "Cat")),
                Criar<IMarcasRepository>(h => h["ObterMarcaPorId"] = _ => new Marcas(1, "Marca")),
                Criar<IUnidadesMedidaRepository>(h => h["ObterUnidadeMedidaPorId"] = a => (int)a![0]! == 1 ? un : kg),
                Criar<ISkuAtributosChavesRepository>(),
                Criar<ISkusRepository>(h =>
                {
                    h["ObterSkusPorProduto"] = _ => new ResultadoPaginado<Skus>(new[] { sku }, 1, 1, 20);
                    h["ObterSkusComMovimentacoesEstoque"] = a => comMovimentacao ? ((IEnumerable<string>)a![0]!).Where(x => x == "SKU-1").ToArray() : Array.Empty<string>();
                }),
                Uow);
        }

        public Task<Resultado<Produtos>> Atualizar(int unidadeId)
            => Service.AtualizarProduto(10, new AtualizarProdutoCommand("Produto", null, 1, 1, unidadeId, true,
                new() { new AtualizarSkuCommand("SKU-1", 10m, "", true, new()) }));
    }

    [Fact]
    public async Task Troca_com_movimentacao_falha_e_nao_atualiza()
    {
        var c = new Cenario(comMovimentacao: true);
        var r = await c.Atualizar(2);

        Assert.False(r.Success);
        Assert.Equal("UNIDADE_MEDIDA_COM_MOVIMENTACAO", r.Errors!.First().Code);
        Assert.False(c.Atualizou);
        Assert.Equal(1, c.Uow.Rollbacks);
    }

    [Fact]
    public async Task Troca_sem_movimentacao_funciona()
    {
        var c = new Cenario(comMovimentacao: false);
        var r = await c.Atualizar(2);

        Assert.True(r.Success);
        Assert.True(c.Atualizou);
    }

    [Fact]
    public async Task Mesma_unidade_com_movimentacao_funciona()
    {
        var c = new Cenario(comMovimentacao: true);
        var r = await c.Atualizar(1);

        Assert.True(r.Success);
        Assert.True(c.Atualizou);
    }
}
