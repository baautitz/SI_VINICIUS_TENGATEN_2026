using System.Reflection;
using Backend.Core.Common.Interfaces;
using Backend.Core.Features.Catalogo.Entities;
using Backend.Core.Features.Catalogo.Repositories;
using Backend.Core.Features.Compras.Commands;
using Backend.Core.Features.Compras.Repositories;
using Backend.Core.Features.Compras.Services;
using Backend.Core.Features.Estoque.Services;
using Backend.Core.Features.Financeiro.Commands;
using Backend.Core.Features.Financeiro.Repositories;
using Backend.Core.Features.Parceiros.Entities;
using Backend.Core.Features.Parceiros.Repositories;
using Backend.Core.Features.Acesso.Repositories;
using Backend.Core.Features.Estoque.Repositories;

namespace Backend.Tests;

public class ComprasServiceTests
{
    // Stub genérico: métodos retornam default (Task incluso) salvo o que o handler devolver.
    public class Stub : DispatchProxy
    {
        public Dictionary<string, Func<object?[]?, object?>> Handlers { get; } = new();

        protected override object? Invoke(MethodInfo? m, object?[]? args)
        {
            object? valor = Handlers.TryGetValue(m!.Name, out var h) ? h(args) : null;
            var ret = m.ReturnType;
            if (ret == typeof(Task)) return Task.CompletedTask;
            if (ret.IsGenericType && ret.GetGenericTypeDefinition() == typeof(Task<>))
                return typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(ret.GetGenericArguments()[0]).Invoke(null, new[] { valor ?? Padrao(ret.GetGenericArguments()[0]) });
            return valor;
        }

        private static object? Padrao(Type t) => t.IsValueType ? Activator.CreateInstance(t) : null;
    }

    internal static T Criar<T>(Action<Dictionary<string, Func<object?[]?, object?>>>? config = null) where T : class
    {
        var p = DispatchProxy.Create<T, Stub>();
        config?.Invoke(((Stub)(object)p).Handlers);
        return p;
    }

    internal static T Instancia<T>() => (T)Activator.CreateInstance(typeof(T), nonPublic: true)!;

    internal class UowFake : IUnitOfWork
    {
        public int Commits, Rollbacks;
        public void BeginTransaction() { }
        public void Commit() => Commits++;
        public void Rollback() => Rollbacks++;
        public void Dispose() { }
    }

    [Fact]
    public async Task Falha_na_gravacao_faz_rollback_e_nao_commita()
    {
        var uow = new UowFake();
        var sku = new Skus("SKU-1", 10m);
        var service = new ComprasService(
            Criar<IComprasRepository>(h => h["CriarCompra"] = _ => throw new InvalidOperationException("falha simulada")),
            Criar<IFornecedoresRepository>(h => h["ObterFornecedorPorId"] = _ => Instancia<Fornecedores>()),
            Criar<IEmitentesRepository>(h => h["ObterEmitentePorId"] = _ => Instancia<Emitentes>()),
            Criar<ITransportadorasRepository>(),
            Criar<ISkusRepository>(h => h["ObterSkuPorSku"] = _ => sku),
            Criar<ICondicoesPagamentosRepository>(),
            new MovimentacoesEstoquesService(Criar<IMovimentacoesEstoquesRepository>(), Criar<ISkusRepository>(), Criar<IUsuariosRepository>(), uow),
            Criar<IContasPagarRepository>(),
            Criar<Backend.Core.Features.NFe.Repositories.INfesRepository>(),
            Criar<IUsuariosRepository>(),
            uow);

        var command = new CriarCompraCommand(
            DateTime.UtcNow, 1, 1, 0, 0, 0, 0, 0, null, null, null,
            new[] { new CriarCompraItemCommand("SKU-1", 2, 5m, 0, null) },
            null,
            new[] { new CriarParcelaPagarCommand(1, DateTime.UtcNow.AddDays(30), 10m) });

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CriarCompra(command));

        Assert.Equal(1, uow.Rollbacks);
        Assert.Equal(0, uow.Commits);
    }

    private static ComprasService ServicoSemRepos() => new(
        Criar<IComprasRepository>(), Criar<IFornecedoresRepository>(), Criar<IEmitentesRepository>(), Criar<ITransportadorasRepository>(),
        Criar<ISkusRepository>(), Criar<ICondicoesPagamentosRepository>(),
        new MovimentacoesEstoquesService(Criar<IMovimentacoesEstoquesRepository>(), Criar<ISkusRepository>(), Criar<IUsuariosRepository>(), new UowFake()),
        Criar<IContasPagarRepository>(), Criar<Backend.Core.Features.NFe.Repositories.INfesRepository>(), Criar<IUsuariosRepository>(), new UowFake());

    private static CriarCompraCommand CompraComParcela(DateTime venc, decimal valor)
        => new(DateTime.UtcNow, 1, 1, 0, 0, 0, 0, 0, null, null, null,
            new[] { new CriarCompraItemCommand("SKU-1", 2, 5m, 0, null) }, null,
            new[] { new CriarParcelaPagarCommand(1, venc, valor) });

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task ParcelaComValorZeroOuNegativoEhRejeitada(int valor)
    {
        var r = await ServicoSemRepos().GravarCompra(CompraComParcela(DateTime.UtcNow.AddDays(5), valor));
        Assert.Contains(r.Errors!, e => e.Code == "PARCELA_VALOR_INVALIDO");
    }

    [Fact]
    public async Task VencimentoAnteriorAChegadaEhRejeitadoNaCompraManualMasNaoNoXml()
    {
        var cmd = CompraComParcela(DateTime.UtcNow.AddDays(-5), 10m);
        var manual = await ServicoSemRepos().GravarCompra(cmd);
        Assert.Contains(manual.Errors!, e => e.Code == "PARCELA_VENCIMENTO_ANTERIOR_CHEGADA");

        var xml = await ServicoSemRepos().GravarCompra(cmd, DateTime.UtcNow.AddDays(-10));
        Assert.DoesNotContain(xml.Errors ?? Enumerable.Empty<Backend.Core.Common.Results.ResultadoErro>(), e => e.Code == "PARCELA_VENCIMENTO_ANTERIOR_CHEGADA");
    }
}
