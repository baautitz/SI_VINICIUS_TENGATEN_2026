using Backend.Core.Features.Catalogo.Commands;
using Backend.Core.Features.Catalogo.Entities;
using Backend.Core.Features.Catalogo.Repositories;
using Backend.Core.Features.Catalogo.Services;

namespace Backend.Tests;

// Valor de atributo ligado a SKU não pode sair por edição do atributo, nem o atributo ser excluído.
public class AtributosSkuVinculoTests
{
    private static SkuAtributosChaves Atributo()
    {
        var a = new SkuAtributosChaves(1, "Tamanho");
        a.AdicionarValor(new SkuAtributosValores(10, 1, "P"));
        a.AdicionarValor(new SkuAtributosValores(11, 1, "M"));
        return a;
    }

    private static SkuAtributosChavesService Servico(Dictionary<int, int> vinculos, bool[] atualizou)
        => new(ComprasServiceTests.Criar<ISkuAtributosChavesRepository>(h =>
        {
            h["ObterAtributoPorId"] = _ => Atributo();
            h["ExisteChave"] = _ => false;
            h["ContarSkusPorValor"] = _ => vinculos;
            h["ContarSkusPorChave"] = _ => vinculos.Count > 0 ? 3 : 0;
            h["AtualizarAtributo"] = a => { atualizou[0] = true; return Atributo(); };
        }));

    [Fact]
    public async Task RemoverValorComSku_Falha_ENaoAtualiza()
    {
        var atualizou = new bool[1];
        var r = await Servico(new() { [11] = 2 }, atualizou)
            .AtualizarAtributo(1, new AtualizarSkuAtributosChavesCommand("Tamanho", new() { new(10, "P") }));

        Assert.False(r.Success);
        Assert.Equal("VALOR_VINCULADO_A_SKU", r.Errors!.First().Code);
        Assert.False(atualizou[0]);
    }

    [Fact]
    public async Task RemoverValorSemSku_Atualiza()
    {
        var atualizou = new bool[1];
        var r = await Servico(new(), atualizou)
            .AtualizarAtributo(1, new AtualizarSkuAtributosChavesCommand("Tamanho", new() { new(10, "P") }));

        Assert.True(r.Success);
        Assert.True(atualizou[0]);
    }

    [Fact]
    public async Task ManterTodosOsValores_NaoConsultaVinculos_EAtualiza()
    {
        var atualizou = new bool[1];
        var r = await Servico(new() { [10] = 5, [11] = 2 }, atualizou)
            .AtualizarAtributo(1, new AtualizarSkuAtributosChavesCommand("Tamanho", new() { new(10, "p"), new(11, "M"), new(null, "G") }));

        Assert.True(r.Success);
        Assert.True(atualizou[0]);
    }

    [Fact]
    public async Task RenomearValorComSku_MantemId_Atualiza()
    {
        var atualizou = new bool[1];
        var r = await Servico(new() { [10] = 5, [11] = 2 }, atualizou)
            .AtualizarAtributo(1, new AtualizarSkuAtributosChavesCommand("Tamanho", new() { new(10, "P"), new(11, "Médio") }));

        Assert.True(r.Success);
        Assert.True(atualizou[0]);
    }

    [Fact]
    public async Task ExcluirAtributoComValorVinculado_Falha()
    {
        var r = await Servico(new() { [10] = 1 }, new bool[1]).DeletarAtributo(1);

        Assert.False(r.Success);
        Assert.Equal("ATRIBUTO_EM_USO", r.Errors!.First().Code);
    }
}
