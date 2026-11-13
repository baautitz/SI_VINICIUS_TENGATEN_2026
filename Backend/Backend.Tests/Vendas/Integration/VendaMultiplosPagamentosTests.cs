using Backend.Core.Common.Results;
using Backend.Core.Features.Financeiro.Commands;
using Backend.Core.Features.Vendas.Commands;
using Backend.Core.Features.Vendas.Entities;
using Backend.Core.Features.Vendas.Services;
using Backend.Infrastructure.PostgreSQL;
using Dapper;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.Vendas.Integration;

using Backend.Tests.Common;

[Trait("Category", "Integration")]
[Collection(PostgresCollection.Nome)]
public class VendaMultiplosPagamentosTests
{
    private const string Sku = "SKU-PAG";
    private readonly PostgresFixture _db;
    private readonly IServiceProvider _provider;

    public VendaMultiplosPagamentosTests(PostgresFixture db)
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

    // Cadastros e 3 condicoes: a vista (entrada 100%), 2x sem entrada e 2x com entrada minima de 30%.
    private async Task<(int Emitente, int Cliente, int AVista, int DuasX, int DuasXEntrada30)> Preparar()
    {
        await _db.Executar("INSERT INTO emitentes (tipo_pessoa, nome_razaosocial, cpf_cnpj, nacionalidade_id) VALUES ('JURIDICA', 'Emitente', '45723174000110', (SELECT id FROM paises LIMIT 1));");
        await _db.Executar("INSERT INTO clientes (tipo_pessoa, nome_razaosocial, cpf_cnpj, nacionalidade_id) VALUES ('FISICA', 'Maria', '52998224725', (SELECT id FROM paises LIMIT 1));");
        await _db.Executar("INSERT INTO unidades_medida (sigla, descricao, categoria) VALUES ('UNX', 'Unidade', 'CONTAGEM') ON CONFLICT DO NOTHING;");
        await _db.Executar("INSERT INTO categorias (categoria) VALUES ('Cat') ON CONFLICT DO NOTHING; INSERT INTO marcas (marca) VALUES ('Marca') ON CONFLICT DO NOTHING;");
        await _db.Executar("INSERT INTO produtos (produto, descricao, categoria_id, marca_id, unidade_medida_id) VALUES ('Prod', 'Desc', (SELECT id FROM categorias LIMIT 1), (SELECT id FROM marcas LIMIT 1), (SELECT id FROM unidades_medida WHERE sigla = 'UNX'));");
        await _db.Executar($"INSERT INTO skus (sku, produto_id, preco, estoque, custo_medio) VALUES ('{Sku}', (SELECT id FROM produtos LIMIT 1), 10, 50, 4);");
        await _db.Executar("INSERT INTO metodos_pagamento (codigo, descricao) VALUES ('99', 'Outros') ON CONFLICT DO NOTHING;");
        await _db.Executar("INSERT INTO condicoes_pagamentos (descricao, metodo_pagamento_codigo, entrada_minima_percentual) VALUES ('A vista', '99', 100), ('2x', '99', 0), ('2x entrada 30', '99', 30);");

        using var s = _db.NovaSessao();
        async Task<int> Id(string d) => await s.Connection.ExecuteScalarAsync<int>("SELECT id FROM condicoes_pagamentos WHERE descricao = @d;", new { d });
        return (
            await s.Connection.ExecuteScalarAsync<int>("SELECT id FROM emitentes LIMIT 1;"),
            await s.Connection.ExecuteScalarAsync<int>("SELECT id FROM clientes LIMIT 1;"),
            await Id("A vista"), await Id("2x"), await Id("2x entrada 30"));
    }

    private static CriarParcelaReceberCommand P(int n, decimal v) => new(n, DateTime.UtcNow.AddDays(30 * n), v);

    private Task<Resultado<Venda>> Vender(int emitente, int cliente, params CriarVendaPagamentoCommand[] pagamentos)
        => Servico(v => v.CriarVenda(new CriarVendaCommand(DateTime.UtcNow, emitente, cliente, null,
            new[] { new CriarVendaItemCommand(Sku, 10, 10, 0) }, pagamentos)));

    private async Task<long> Escalar(string sql)
    {
        using var s = _db.NovaSessao();
        return await s.Connection.ExecuteScalarAsync<long>(sql);
    }

    [Fact]
    public async Task Entrada_a_vista_mais_parcelado_nasce_recebida_e_cada_linha_guarda_sua_condicao()
    {
        var c = await Preparar();
        var r = await Vender(c.Emitente, c.Cliente,
            new CriarVendaPagamentoCommand(c.AVista, 30, new[] { P(1, 30) }),
            new CriarVendaPagamentoCommand(c.DuasX, 70, new[] { P(1, 35), P(2, 35) }));
        Assert.True(r.Success, r.Errors?.FirstOrDefault()?.Message);

        using var s = _db.NovaSessao();
        var contas = (await s.Connection.QueryAsync("SELECT numero_documento AS doc, numero_parcela AS n, total_parcelas AS t, valor, valor_pago AS pago, status::text AS st, condicao_pagamento_id AS cond FROM contas_receber ORDER BY numero_documento, numero_parcela;")).ToList();
        Assert.Equal(3, contas.Count);
        Assert.Equal("VENDA-1-1", (string)contas[0].doc);
        Assert.Equal(30m, (decimal)contas[0].pago);
        Assert.Equal(c.AVista, (int)contas[0].cond);
        Assert.Equal("PAGO", ((string)contas[0].st).ToUpperInvariant());
        Assert.All(contas.Skip(1), x =>
        {
            Assert.Equal("VENDA-1-2", (string)x.doc);
            Assert.Equal(2, (int)x.t);
            Assert.Equal(0m, (decimal)x.pago);
            Assert.Equal(c.DuasX, (int)x.cond);
        });
    }

    [Fact]
    public async Task Multiplas_linhas_a_vista_ficam_todas_recebidas()
    {
        var c = await Preparar();
        var r = await Vender(c.Emitente, c.Cliente,
            new CriarVendaPagamentoCommand(c.AVista, 60, new[] { P(1, 60) }),
            new CriarVendaPagamentoCommand(c.AVista, 40, new[] { P(1, 40) }));
        Assert.True(r.Success, r.Errors?.FirstOrDefault()?.Message);
        Assert.Equal(2, await Escalar("SELECT count(*) FROM contas_receber WHERE valor_pago = valor;"));
    }

    [Fact]
    public async Task Soma_diferente_do_total_nao_cria_venda()
    {
        var c = await Preparar();
        var r = await Vender(c.Emitente, c.Cliente, new CriarVendaPagamentoCommand(c.AVista, 90, new[] { P(1, 90) }));
        Assert.False(r.Success);
        Assert.Equal(0, await Escalar("SELECT count(*) FROM vendas;"));
    }

    [Fact]
    public async Task Entrada_minima_vale_sobre_a_soma_das_linhas_a_vista()
    {
        var c = await Preparar();
        var curta = await Vender(c.Emitente, c.Cliente,
            new CriarVendaPagamentoCommand(c.AVista, 20, new[] { P(1, 20) }),
            new CriarVendaPagamentoCommand(c.DuasXEntrada30, 80, new[] { P(1, 40), P(2, 40) }));
        Assert.False(curta.Success);
        Assert.Equal("ENTRADA_MINIMA_NAO_ATINGIDA", curta.Errors!.First().Code);

        var ok = await Vender(c.Emitente, c.Cliente,
            new CriarVendaPagamentoCommand(c.AVista, 15, new[] { P(1, 15) }),
            new CriarVendaPagamentoCommand(c.AVista, 15, new[] { P(1, 15) }),
            new CriarVendaPagamentoCommand(c.DuasXEntrada30, 70, new[] { P(1, 35), P(2, 35) }));
        Assert.True(ok.Success, ok.Errors?.FirstOrDefault()?.Message);
    }

    [Fact]
    public async Task Condicao_inativa_e_recusada()
    {
        var c = await Preparar();
        await _db.Executar($"UPDATE condicoes_pagamentos SET ativo = FALSE WHERE id = {c.DuasX};");
        var r = await Vender(c.Emitente, c.Cliente, new CriarVendaPagamentoCommand(c.DuasX, 100, new[] { P(1, 50), P(2, 50) }));
        Assert.False(r.Success);
        Assert.Equal("CONDICAO_PAGAMENTO_INATIVA", r.Errors!.First().Code);
    }
}
