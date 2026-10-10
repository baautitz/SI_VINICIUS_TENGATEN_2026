using System.Reflection;
using Backend.Core.Features.Financeiro.Entities;
using Backend.Core.Features.Financeiro.Repositories;
using Backend.Core.Features.Localizacao.Entities;
using Backend.Core.Features.Localizacao.Repositories;
using Backend.Core.Features.Parceiros.Commands;
using Backend.Core.Features.Parceiros.Enums;
using Backend.Core.Features.Parceiros.Repositories;
using Backend.Core.Features.Parceiros.Services;

namespace Backend.Tests;

public class FornecedoresCondicaoTests
{
    private static T Criar<T>(Action<Dictionary<string, Func<object?[]?, object?>>> config) where T : class
    {
        var p = DispatchProxy.Create<T, ComprasServiceTests.Stub>();
        config(((ComprasServiceTests.Stub)(object)p).Handlers);
        return p;
    }

    private static async Task<string?> CriarComCondicao(CondicoesPagamentos? condicao)
    {
        var service = new FornecedoresService(
            Criar<IFornecedoresRepository>(_ => { }),
            Criar<IBairrosRepository>(h => h["ObterBairroPorId"] = _ => (Bairros)Activator.CreateInstance(typeof(Bairros), nonPublic: true)!),
            Criar<IPaisesRepository>(h => h["ObterPaisPorId"] = _ => (Paises)Activator.CreateInstance(typeof(Paises), nonPublic: true)!),
            Criar<ICondicoesPagamentosRepository>(h => h["ObterCondicaoPagamentoPorId"] = _ => condicao));
        var r = await service.CriarFornecedor(new CriarFornecedorCommand(TipoPessoa.JURIDICA, "Forn", "123", null, null, "Rua A", "1", 1, 1, "11999999999", "f@f.com", null, true, null, new DateTime(2000, 1, 1), 5));
        return r.Errors?.FirstOrDefault()?.Code;
    }

    [Fact]
    public async Task CondicaoInexistente_Falha() => Assert.Equal("CONDICAO_PAGAMENTO_INEXISTENTE", await CriarComCondicao(null));

    [Fact]
    public async Task CondicaoInativa_Falha() =>
        Assert.Equal("CONDICAO_PAGAMENTO_INATIVA", await CriarComCondicao((CondicoesPagamentos)Activator.CreateInstance(typeof(CondicoesPagamentos), nonPublic: true)!));
}
