using Backend.Core.Features.Catalogo.Commands;
using Backend.Core.Features.Catalogo.Entities;
using Backend.Core.Features.Catalogo.Repositories;
using Backend.Core.Features.Catalogo.Services;
using static Backend.Tests.ComprasServiceTests;

namespace Backend.Tests;

// "Permite decimais" não pode ser desmarcado numa unidade cujos produtos já têm movimentação de estoque.
public class UnidadesMedidaDecimaisTests
{
    private static (UnidadesMedidaService Service, bool[] Atualizou) Cenario(bool permiteDecimais, bool emUso)
    {
        var atualizou = new bool[1];
        var un = new UnidadesMedida("KG", "Quilo", "PESO", permiteDecimais) { Id = 1 };
        var service = new UnidadesMedidaService(Criar<IUnidadesMedidaRepository>(h =>
        {
            h["ObterUnidadeMedidaPorId"] = _ => un;
            h["ExisteSigla"] = _ => false;
            h["UnidadeTemMovimentacao"] = _ => emUso;
            h["AtualizarUnidadeMedida"] = a => { atualizou[0] = true; return a![1]; };
        }));
        return (service, atualizou);
    }

    private static AtualizarUnidadeMedidaCommand Comando(bool permiteDecimais)
        => new("KG", "Quilo", "PESO", permiteDecimais, true);

    [Fact]
    public async Task Desmarcar_decimais_em_unidade_com_movimentacao_falha()
    {
        var (service, atualizou) = Cenario(permiteDecimais: true, emUso: true);
        var r = await service.AtualizarUnidadeMedida(1, Comando(false));

        Assert.False(r.Success);
        Assert.Equal("UNIDADE_MEDIDA_EM_USO", r.Errors!.First().Code);
        Assert.False(atualizou[0]);
    }

    [Fact]
    public async Task Desmarcar_decimais_sem_movimentacao_funciona()
    {
        var (service, atualizou) = Cenario(permiteDecimais: true, emUso: false);

        Assert.True((await service.AtualizarUnidadeMedida(1, Comando(false))).Success);
        Assert.True(atualizou[0]);
    }

    [Fact]
    public async Task Marcar_decimais_com_movimentacao_funciona()
    {
        var (service, atualizou) = Cenario(permiteDecimais: false, emUso: true);

        Assert.True((await service.AtualizarUnidadeMedida(1, Comando(true))).Success);
        Assert.True(atualizou[0]);
    }
}
