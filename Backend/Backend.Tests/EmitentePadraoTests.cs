using System.Reflection;
using Backend.Core.Features.Localizacao.Entities;
using Backend.Core.Features.Localizacao.Repositories;
using Backend.Core.Features.Parceiros.Commands;
using Backend.Core.Features.Parceiros.Enums;
using Backend.Core.Features.Parceiros.Repositories;
using Backend.Core.Features.Parceiros.Services;

namespace Backend.Tests;

public class EmitentePadraoTests
{
    private static T Criar<T>(Action<Dictionary<string, Func<object?[]?, object?>>> config) where T : class
    {
        var p = DispatchProxy.Create<T, ComprasServiceTests.Stub>();
        config(((ComprasServiceTests.Stub)(object)p).Handlers);
        return p;
    }

    private static EmitentesService CriarService() => new(
        Criar<IEmitentesRepository>(h =>
        {
            h["ExisteEmitenteCpfCnpj"] = _ => false;
            h["CriarEmitente"] = a => a![0];
        }),
        Criar<IBairrosRepository>(_ => { }),
        Criar<IPaisesRepository>(h => h["ObterPaisPorId"] = _ => new Paises(1, new Backend.Core.Common.ValueObjects.Ddi("55"), "BRA", "BRL", "R$", "Brasil")));

    private static CriarEmitenteCommand Comando(bool ativo, bool padrao) => new(
        TipoPessoa.JURIDICA, "Emitente", "11222333000181", null, null, null, null, 1,
        null, null, null, null, null, null, ativo, Padrao: padrao);

    [Fact]
    public async Task CriarEmitente_padrao_e_ativo_marca_padrao()
    {
        var r = await CriarService().CriarEmitente(Comando(ativo: true, padrao: true));

        Assert.True(r.Success);
        Assert.True(r.Data!.Padrao);
    }

    [Fact]
    public async Task CriarEmitente_padrao_inativo_e_rejeitado()
    {
        var r = await CriarService().CriarEmitente(Comando(ativo: false, padrao: true));

        Assert.False(r.Success);
        Assert.Equal("EMITENTE_PADRAO_INATIVO", r.Errors!.First().Code);
    }

    [Fact]
    public void Desativar_emitente_padrao_e_rejeitado()
    {
        var emitente = new Backend.Core.Features.Parceiros.Entities.Emitentes(
            TipoPessoa.JURIDICA, "Emitente",
            Backend.Core.Common.ValueObjects.Documento.Criar("11222333000181", "BRA", TipoPessoa.JURIDICA),
            new Paises(1, new Backend.Core.Common.ValueObjects.Ddi("55"), "BRA", "BRL", "R$", "Brasil"),
            padrao: true);

        Assert.Throws<Backend.Core.Common.Exceptions.DomainException>(() => emitente.Desativar());
    }
}
