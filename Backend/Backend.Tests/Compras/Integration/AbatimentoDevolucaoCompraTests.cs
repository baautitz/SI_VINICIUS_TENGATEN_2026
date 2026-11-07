using Backend.Core.Features.Compras.Commands;
using Backend.Core.Features.Compras.Services;
using Backend.Core.Features.Financeiro.Commands;
using Backend.Infrastructure.PostgreSQL;
using Backend.Tests.Common;
using Dapper;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.Compras.Integration;

[Trait("Category", "Integration")]
[Collection(PostgresCollection.Nome)]
public class AbatimentoDevolucaoCompraTests
{
    private const string Sku = "SKU-DC";
    private readonly PostgresFixture _db;
    private readonly IServiceProvider _provider;

    public AbatimentoDevolucaoCompraTests(PostgresFixture db)
    {
        _db = db;
        _provider = new ServiceCollection().AddPostgreSQLInfrastructure(db.ConnectionString).BuildServiceProvider();
        _db.Limpar().GetAwaiter().GetResult();
        _db.Executar("TRUNCATE devolucoes_compra, compras, emitentes, fornecedores, skus, produtos, movimentacoes_estoque RESTART IDENTITY CASCADE;").GetAwaiter().GetResult();
    }

    private async Task<T> Servico<T, TS>(Func<TS, Task<T>> acao) where TS : notnull
    {
        using var escopo = _provider.CreateScope();
        return await acao(escopo.ServiceProvider.GetRequiredService<TS>());
    }

    // Compra de 10 un a R$ 10 (R$ 100) em 2 parcelas de 50; o vencimento da parcela 1 vem em dias.
    private async Task<(int CompraId, int ItemId, int FornecedorId)> CompraDe2Parcelas(int diasParcela1 = 30)
    {
        await _db.Executar("INSERT INTO emitentes (tipo_pessoa, nome_razaosocial, cpf_cnpj, nacionalidade_id) VALUES ('JURIDICA', 'Emitente', '45723174000110', (SELECT id FROM paises LIMIT 1));");
        await _db.Executar("INSERT INTO fornecedores (tipo_pessoa, nome_razaosocial, cpf_cnpj, nacionalidade_id) VALUES ('JURIDICA', 'Fornecedor Z', '11222333000181', (SELECT id FROM paises LIMIT 1));");
        await _db.Executar("INSERT INTO unidades_medida (sigla, descricao, categoria) VALUES ('UNX', 'Unidade', 'CONTAGEM') ON CONFLICT DO NOTHING;");
        await _db.Executar("INSERT INTO categorias (categoria) VALUES ('Cat') ON CONFLICT DO NOTHING; INSERT INTO marcas (marca) VALUES ('Marca') ON CONFLICT DO NOTHING;");
        await _db.Executar("INSERT INTO produtos (produto, descricao, categoria_id, marca_id, unidade_medida_id) VALUES ('Prod', 'Desc', (SELECT id FROM categorias LIMIT 1), (SELECT id FROM marcas LIMIT 1), (SELECT id FROM unidades_medida WHERE sigla = 'UNX'));");
        await _db.Executar($"INSERT INTO skus (sku, produto_id, preco, estoque, custo_medio) VALUES ('{Sku}', (SELECT id FROM produtos LIMIT 1), 20, 0, 0);");

        var emitente = await Escalar<int>("SELECT id FROM emitentes LIMIT 1;");
        var fornecedor = await Escalar<int>("SELECT id FROM fornecedores LIMIT 1;");

        var compra = await Servico<Backend.Core.Common.Results.Resultado<Backend.Core.Features.Compras.Entities.Compra>, ComprasService>(c => c.CriarCompra(new CriarCompraCommand(
            DateTime.UtcNow, emitente, fornecedor, 0, 0, 0, 0, 0, null, null, null,
            new[] { new CriarCompraItemCommand(Sku, 10, 10, 0, null) }, null,
            new[] { new CriarParcelaPagarCommand(1, DateTime.UtcNow.Date.AddDays(diasParcela1), 50), new CriarParcelaPagarCommand(2, DateTime.UtcNow.Date.AddDays(60), 50) })));
        Assert.True(compra.Success, compra.Errors?.FirstOrDefault()?.Message);

        return (compra.Data!.Id, await Escalar<int>("SELECT id FROM compras_itens LIMIT 1;"), fornecedor);
    }

    private async Task<int> Devolver(int compraId, int itemId, decimal quantidade)
    {
        var r = await Servico<Backend.Core.Common.Results.Resultado<Backend.Core.Features.Compras.Entities.DevolucaoCompra>, DevolucoesCompraService>(
            s => s.CriarDevolucao(compraId, new CriarDevolucaoCompraCommand("Produto com defeito", new[] { new CriarDevolucaoCompraItemCommand(itemId, quantidade) })));
        Assert.True(r.Success, r.Errors?.FirstOrDefault()?.Message);
        return r.Data!.Id;
    }

    // (status, valor, pago, abatido)
    private async Task<(string, decimal, decimal, decimal)> Parcela(int n)
    {
        using var s = _db.NovaSessao();
        var r = await s.Connection.QuerySingleAsync($"SELECT status::text AS st, valor AS v, valor_pago AS p, valor_abatido AS a FROM contas_pagar WHERE origem_tipo::text = 'COMPRA' AND numero_parcela = {n};");
        return ((string)r.st, (decimal)r.v, (decimal)r.p, (decimal)r.a);
    }

    private async Task<T> Escalar<T>(string sql)
    {
        using var s = _db.NovaSessao();
        return await s.Connection.ExecuteScalarAsync<T>(sql);
    }

    [Fact]
    public async Task Devolucao_abate_a_parcela_vencida_primeiro_e_registra_o_abatimento()
    {
        var (compraId, itemId, _) = await CompraDe2Parcelas(diasParcela1: 30);
        await _db.Executar("UPDATE contas_pagar SET data_vencimento = CURRENT_DATE + 90 WHERE numero_parcela = 1; UPDATE contas_pagar SET data_vencimento = CURRENT_DATE - 10 WHERE numero_parcela = 2;");

        var devolucaoId = await Devolver(compraId, itemId, 6); // R$ 60

        Assert.Equal(("PAGO", 50m, 0m, 50m), await Parcela(2));
        Assert.Equal(("PARCIAL", 50m, 0m, 10m), await Parcela(1));
        Assert.Equal(2, await Escalar<int>($"SELECT COUNT(*) FROM devolucao_compra_abatimentos WHERE devolucao_compra_id = {devolucaoId} AND estornado_em IS NULL;"));
        Assert.Equal(60m, await Escalar<decimal>("SELECT SUM(valor) FROM devolucao_compra_abatimentos;"));
        Assert.Equal(0, await Escalar<int>("SELECT COUNT(*) FROM contas_receber;"));
    }

    [Fact]
    public async Task Excedente_do_aberto_vira_reembolso_a_receber_do_fornecedor_sem_consumir_o_pago()
    {
        var (compraId, itemId, fornecedorId) = await CompraDe2Parcelas();
        await _db.Executar("UPDATE contas_pagar SET valor_pago = 50, status = 'PAGO' WHERE numero_parcela = 1;");

        var devolucaoId = await Devolver(compraId, itemId, 8); // R$ 80: abate 50 (aberto) e reembolsa 30

        Assert.Equal(("PAGO", 50m, 50m, 0m), await Parcela(1));
        Assert.Equal(("PAGO", 50m, 0m, 50m), await Parcela(2));

        using var s = _db.NovaSessao();
        var r = await s.Connection.QuerySingleAsync("SELECT valor AS v, origem_tipo::text AS o, origem_id AS oid, fornecedor_id AS f, cliente_id AS c, total_parcelas AS tp, data_vencimento = CURRENT_DATE AS hoje FROM contas_receber;");
        Assert.Equal(30m, (decimal)r.v);
        Assert.Equal("DEVOLUCAO_COMPRA", (string)r.o);
        Assert.Equal(devolucaoId, (int)r.oid);
        Assert.Equal(fornecedorId, (int)r.f);
        Assert.Null((int?)r.c);
        Assert.Equal(1, (int)r.tp);
    }
}
