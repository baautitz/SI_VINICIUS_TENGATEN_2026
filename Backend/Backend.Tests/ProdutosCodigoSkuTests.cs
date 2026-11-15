using Backend.Core.Common.Results;
using Backend.Core.Features.Catalogo.Commands;
using Backend.Core.Features.Catalogo.Entities;
using Backend.Core.Features.Catalogo.Repositories;
using Backend.Core.Features.Catalogo.Services;
using Backend.Tests.Common;
using static Backend.Tests.Common.Fakes;

namespace Backend.Tests;

// Codigo gerado do SKU ({produto}{indice}) pula os codigos que ja existem, em vez de estourar na chave primaria.
public class ProdutosCodigoSkuTests
{
    private static readonly UnidadesMedida Un = new("UN", "UN", "CONTAGEM", false) { Id = 1 };

    private static (ProdutosService Service, List<string> Criados) Cenario(params string[] existentes)
    {
        var produto = new Produtos(10, "Produto", null, new Categorias(1, "Cat"), new Marcas(1, "Marca"), Un);
        var skus = existentes.Select(c => new Skus(c, 10m, estoque: 0)).ToList();
        var criados = new List<string>();

        var service = new ProdutosService(
            Criar<IProdutosRepository>(h =>
            {
                h["ObterProdutoPorId"] = _ => produto;
                h["ExisteProduto"] = _ => false;
                h["AtualizarProduto"] = a => a![1];
                h["CriarProduto"] = _ => produto;
            }),
            Criar<ICategoriasRepository>(h => h["ObterCategoriaPorId"] = _ => new Categorias(1, "Cat")),
            Criar<IMarcasRepository>(h => h["ObterMarcaPorId"] = _ => new Marcas(1, "Marca")),
            Criar<IUnidadesMedidaRepository>(h => h["ObterUnidadeMedidaPorId"] = _ => Un),
            Criar<ISkuAtributosChavesRepository>(),
            Criar<ISkusRepository>(h =>
            {
                h["ObterSkusPorProduto"] = _ => new ResultadoPaginado<Skus>(skus.ToArray(), skus.Count, 1, 20);
                h["ObterSkusComMovimentacoesEstoque"] = _ => Array.Empty<string>();
                h["ObterSkuPorSku"] = a => skus.FirstOrDefault(s => s.Sku == (string)a![0]!);
                h["CriarSku"] = a =>
                {
                    var sku = (Skus)a![1]!;
                    skus.Add(sku);
                    criados.Add(sku.Sku);
                    return sku;
                };
            }),
            new UowFake());

        return (service, criados);
    }

    [Fact]
    public async Task Edicao_pula_codigo_gerado_que_ja_existe()
    {
        // 1 SKU existente ("102") => indice inicial 2 => "102" colide; o proximo livre e "103".
        var (service, criados) = Cenario("102");

        var r = await service.AtualizarProduto(10, new AtualizarProdutoCommand("Produto", null, 1, 1, 1, true,
            new() { new AtualizarSkuCommand("102", 10m, "", true, new()), new AtualizarSkuCommand(null, 10m, "", true, new()) }));

        Assert.True(r.Success);
        Assert.Equal(new[] { "103" }, criados);
    }

    [Fact]
    public async Task Criacao_pula_codigo_digitado_a_mao_que_colide_com_o_gerado()
    {
        var (service, criados) = Cenario("101");

        var r = await service.CriarProduto(new CriarProdutoCommand("Produto", null, 1, 1, 1, true,
            new() { new CriarSkuCommand(null, 10m, "", true, new()) }));

        Assert.True(r.Success);
        Assert.Equal(new[] { "102" }, criados);
    }
}
