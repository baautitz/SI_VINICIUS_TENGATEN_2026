using Backend.Core.Features.Financeiro.Commands;
using Backend.Core.Features.Vendas.Commands;
using Backend.Core.Features.Vendas.Entities.Enums;
using Backend.Core.Features.Vendas.Services;
using Backend.Infrastructure.PostgreSQL;
using Dapper;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.Integracao;

[Collection(PostgresCollection.Nome)]
public class EstornoDevolucaoTests
{
    private const string Sku = "SKU-DEV";
    private readonly PostgresFixture _db;
    private readonly IServiceProvider _provider;

    public EstornoDevolucaoTests(PostgresFixture db)
    {
        _db = db;
        _provider = new ServiceCollection().AddPostgreSQLInfrastructure(db.ConnectionString).BuildServiceProvider();
        _db.Limpar().GetAwaiter().GetResult();
        _db.Executar("TRUNCATE devolucoes, vendas, emitentes, skus, produtos, movimentacoes_estoque, condicoes_pagamentos RESTART IDENTITY CASCADE;").GetAwaiter().GetResult();
    }

    private async Task<T> Servico<T>(Func<VendasService, Task<T>> acao)
    {
        using var escopo = _provider.CreateScope();
        return await acao(escopo.ServiceProvider.GetRequiredService<VendasService>());
    }

    // Venda de 10 un a R$ 10 (estoque 20 -> 10) em 2 parcelas de 50, sem recebimento.
    private async Task<(int VendaId, int ItemId)> VendaDe2Parcelas()
    {
        await _db.Executar("INSERT INTO emitentes (tipo_pessoa, nome_razaosocial, cpf_cnpj, nacionalidade_id) VALUES ('JURIDICA', 'Emitente', '45723174000110', (SELECT id FROM paises LIMIT 1));");
        await _db.Executar("INSERT INTO clientes (tipo_pessoa, nome_razaosocial, cpf_cnpj, nacionalidade_id) VALUES ('FISICA', 'Maria', '52998224725', (SELECT id FROM paises LIMIT 1));");
        await _db.Executar("INSERT INTO unidades_medida (sigla, descricao, categoria) VALUES ('UNX', 'Unidade', 'CONTAGEM') ON CONFLICT DO NOTHING;");
        await _db.Executar("INSERT INTO categorias (categoria) VALUES ('Cat') ON CONFLICT DO NOTHING; INSERT INTO marcas (marca) VALUES ('Marca') ON CONFLICT DO NOTHING;");
        await _db.Executar("INSERT INTO produtos (produto, descricao, categoria_id, marca_id, unidade_medida_id) VALUES ('Prod', 'Desc', (SELECT id FROM categorias LIMIT 1), (SELECT id FROM marcas LIMIT 1), (SELECT id FROM unidades_medida WHERE sigla = 'UNX'));");
        await _db.Executar($"INSERT INTO skus (sku, produto_id, preco, estoque, custo_medio) VALUES ('{Sku}', (SELECT id FROM produtos LIMIT 1), 10, 20, 4);");
        await _db.Executar("INSERT INTO metodos_pagamento (codigo, descricao) VALUES ('99', 'Outros') ON CONFLICT DO NOTHING; INSERT INTO condicoes_pagamentos (descricao, metodo_pagamento_codigo) VALUES ('2x', '99');");

        using var s = _db.NovaSessao();
        var emitente = await s.Connection.ExecuteScalarAsync<int>("SELECT id FROM emitentes LIMIT 1;");
        var cliente = await s.Connection.ExecuteScalarAsync<int>("SELECT id FROM clientes LIMIT 1;");
        var condicao = await s.Connection.ExecuteScalarAsync<int>("SELECT id FROM condicoes_pagamentos LIMIT 1;");

        var venda = await Servico(v => v.CriarVenda(new CriarVendaCommand(DateTime.UtcNow, emitente, cliente, null,
            new[] { new CriarVendaItemCommand(Sku, 10, 10, 0) }, condicao,
            new[] { new CriarParcelaReceberCommand(1, DateTime.UtcNow.AddDays(30), 50), new CriarParcelaReceberCommand(2, DateTime.UtcNow.AddDays(60), 50) })));
        Assert.True(venda.Success, venda.Errors?.FirstOrDefault()?.Message);

        return (venda.Data!.Id, await Escalar<int>("SELECT id FROM vendas_itens LIMIT 1;"));
    }

    // Devolve 6 un (R$ 60): abate 50 da parcela 2 (cancelada) e 10 da parcela 1.
    private async Task<int> Devolver(int vendaId, int itemId)
    {
        var r = await Servico(v => v.CriarDevolucao(vendaId, new CriarDevolucaoCommand("Produto com defeito", new[] { new CriarDevolucaoItemCommand(itemId, 6) })));
        Assert.True(r.Success, r.Errors?.FirstOrDefault()?.Message);
        return r.Data!.Id;
    }

    private async Task<(string, decimal)> Parcela(int n)
    {
        using var s = _db.NovaSessao();
        var r = await s.Connection.QuerySingleAsync($"SELECT status::text AS st, valor AS v FROM contas_receber WHERE numero_parcela = {n};");
        return ((string)r.st, (decimal)r.v);
    }

    private Task<decimal> Estoque() => Escalar<decimal>($"SELECT estoque FROM skus WHERE sku = '{Sku}';");

    private async Task<T> Escalar<T>(string sql)
    {
        using var s = _db.NovaSessao();
        return await s.Connection.ExecuteScalarAsync<T>(sql);
    }

    [Fact]
    public async Task Estorno_reverte_estoque_e_restitui_contas_a_receber()
    {
        var (vendaId, itemId) = await VendaDe2Parcelas();
        var devolucaoId = await Devolver(vendaId, itemId);
        Assert.Equal(16m, await Estoque());
        Assert.Equal("CANCELADO", await Escalar<string>("SELECT status::text FROM contas_receber WHERE numero_parcela = 2;"));
        Assert.Equal(("ABERTO", 40m), await Parcela(1));

        var r = await Servico(v => v.EstornarDevolucao(devolucaoId, new EstornarDevolucaoCommand("Devolucao lancada errada")));

        Assert.True(r.Success, r.Errors?.FirstOrDefault()?.Message);
        Assert.Equal(StatusDevolucao.ESTORNADA, r.Data!.Status);
        Assert.Equal("Devolucao lancada errada", r.Data.MotivoEstorno);
        Assert.NotNull(r.Data.EstornadoEm);

        Assert.Equal(10m, await Estoque());
        Assert.Equal(1, await Escalar<int>($"SELECT COUNT(*) FROM movimentacoes_estoque WHERE origem_tipo::text = 'ESTORNO' AND tipo_movimentacao::text = 'SAIDA';"));

        Assert.Equal(("ABERTO", 50m), await Parcela(2));
        Assert.Equal(("ABERTO", 50m), await Parcela(1));
        Assert.Equal(0, await Escalar<int>("SELECT COUNT(*) FROM contas_pagar;"));

        var venda = await Servico(v => v.ObterVendaPorId(vendaId));
        Assert.Equal(0m, venda!.Itens.Single().QuantidadeDevolvida);
        Assert.Equal(StatusDevolucaoVenda.NENHUMA, venda.StatusDevolucao);

        var lista = await Servico(v => v.PesquisarDevolucoes("", vendaId: vendaId));
        Assert.Equal(StatusDevolucao.ESTORNADA, Assert.Single(lista.Itens).Status);
    }

    [Theory]
    [InlineData(4, "PARCIAL")]
    [InlineData(40, "PAGO")]
    public async Task Estorno_barrado_quando_conta_a_receber_possui_baixa(int valorPago, string status)
    {
        var (vendaId, itemId) = await VendaDe2Parcelas();
        var devolucaoId = await Devolver(vendaId, itemId);
        await _db.Executar("UPDATE contas_receber SET valor_pago = @valorPago, status = @status::status_titulo_financeiro_enum WHERE numero_parcela = 1;", new { valorPago, status });

        var r = await Servico(v => v.EstornarDevolucao(devolucaoId, new EstornarDevolucaoCommand("Devolucao lancada errada")));

        Assert.False(r.Success);
        Assert.Contains("ESTORNO_PARCELA_BAIXADA", r.Errors!.Select(e => e.Code));
        Assert.Contains("Estorne o recebimento primeiro", r.Errors!.First().Message);
        Assert.Equal(16m, await Estoque());
        Assert.Equal("ATIVA", await Escalar<string>("SELECT status FROM devolucoes;"));
        Assert.Equal("CANCELADO", await Escalar<string>("SELECT status::text FROM contas_receber WHERE numero_parcela = 2;"));
        Assert.Equal(status, await Escalar<string>("SELECT status::text FROM contas_receber WHERE numero_parcela = 1;"));
    }

    [Theory]
    [InlineData(4, "PARCIAL")]
    [InlineData(50, "PAGO")]
    public async Task Devolucao_barrada_quando_conta_a_receber_possui_baixa(int valorPago, string status)
    {
        var (vendaId, itemId) = await VendaDe2Parcelas();
        await _db.Executar("UPDATE contas_receber SET valor_pago = @valorPago, status = @status::status_titulo_financeiro_enum WHERE numero_parcela = 1;", new { valorPago, status });

        var r = await Servico(v => v.CriarDevolucao(vendaId, new CriarDevolucaoCommand("Produto com defeito", new[] { new CriarDevolucaoItemCommand(itemId, 6) })));

        Assert.False(r.Success);
        var erro = Assert.Single(r.Errors!);
        Assert.Equal("DEVOLUCAO_PARCELA_BAIXADA", erro.Code);
        Assert.Contains("Estorne o recebimento primeiro", erro.Message);
        Assert.Equal(10m, await Estoque());
        Assert.Equal(0, await Escalar<int>("SELECT COUNT(*) FROM devolucoes;"));
        Assert.Equal(("ABERTO", 50m), await Parcela(2));
        Assert.Equal(0, await Escalar<int>("SELECT COUNT(*) FROM contas_pagar;"));
    }

    [Fact]
    public async Task Estornar_duas_vezes_e_erro_de_validacao()
    {
        var (vendaId, itemId) = await VendaDe2Parcelas();
        var devolucaoId = await Devolver(vendaId, itemId);
        var comando = new EstornarDevolucaoCommand("Devolucao lancada errada");

        Assert.True((await Servico(v => v.EstornarDevolucao(devolucaoId, comando))).Success);
        var segunda = await Servico(v => v.EstornarDevolucao(devolucaoId, comando));

        Assert.False(segunda.Success);
        Assert.Contains(VendasService.DevolucaoJaEstornada, segunda.Errors!.Select(e => e.Code));
        Assert.Equal(10m, await Estoque()); // nao saiu duas vezes
    }

    [Fact]
    public async Task Motivo_invalido_e_devolucao_inexistente_falham()
    {
        Assert.Contains("MOTIVO_INVALIDO", (await Servico(v => v.EstornarDevolucao(1, new EstornarDevolucaoCommand("abc")))).Errors!.Select(e => e.Code));
        Assert.Contains(VendasService.DevolucaoInexistente, (await Servico(v => v.EstornarDevolucao(999, new EstornarDevolucaoCommand("motivo valido")))).Errors!.Select(e => e.Code));
    }
}
