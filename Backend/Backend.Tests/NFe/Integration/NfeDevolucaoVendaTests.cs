using Backend.Core.Features.Financeiro.Commands;
using Backend.Core.Features.NFe.Services;
using Backend.Core.Features.Vendas.Commands;
using Backend.Core.Features.Vendas.Services;
using Backend.Infrastructure.PostgreSQL;
using Dapper;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.NFe.Integration;

using Backend.Tests.Common;

[Trait("Category", "Integration")]
[Collection(PostgresCollection.Nome)]
public class NfeDevolucaoVendaTests
{
    private const string Sku = "SKU-NFD";
    private readonly PostgresFixture _db;
    private readonly IServiceProvider _provider;

    public NfeDevolucaoVendaTests(PostgresFixture db)
    {
        _db = db;
        _provider = new ServiceCollection().AddPostgreSQLInfrastructure(db.ConnectionString).BuildServiceProvider();
        _db.Limpar().GetAwaiter().GetResult();
        _db.Executar("TRUNCATE nfes, devolucoes, vendas, emitentes, skus, produtos, movimentacoes_estoque, condicoes_pagamentos, bairros, cidades, estados RESTART IDENTITY CASCADE;").GetAwaiter().GetResult();
    }

    private async Task<T> Usar<T, S>(Func<S, Task<T>> acao) where S : notnull
    {
        using var escopo = _provider.CreateScope();
        return await acao(escopo.ServiceProvider.GetRequiredService<S>());
    }

    private async Task<T> Escalar<T>(string sql)
    {
        using var s = _db.NovaSessao();
        return await s.Connection.ExecuteScalarAsync<T>(sql);
    }

    // Venda de 10 un a R$ 10 (estoque 20 -> 10) em 2 parcelas de 50; com comNfe, já emite a NF-e de saída da venda.
    private async Task<(int VendaId, int ItemId)> Venda(bool comNfe)
    {
        await _db.Executar("INSERT INTO estados (pais_id, estado, uf) VALUES ((SELECT id FROM paises LIMIT 1), 'Sao Paulo', 'SP');");
        await _db.Executar("INSERT INTO cidades (cidade, ddd, estado_id) VALUES ('Sao Paulo', '11', (SELECT id FROM estados LIMIT 1));");
        await _db.Executar("INSERT INTO bairros (bairro, cidade_id) VALUES ('Centro', (SELECT id FROM cidades LIMIT 1));");
        await _db.Executar("INSERT INTO emitentes (tipo_pessoa, nome_razaosocial, cpf_cnpj, nacionalidade_id, bairro_id) VALUES ('JURIDICA', 'Emitente', '45723174000110', (SELECT id FROM paises LIMIT 1), (SELECT id FROM bairros LIMIT 1));");
        await _db.Executar("INSERT INTO clientes (tipo_pessoa, nome_razaosocial, cpf_cnpj, nacionalidade_id) VALUES ('FISICA', 'Maria', '52998224725', (SELECT id FROM paises LIMIT 1));");
        await _db.Executar("INSERT INTO unidades_medida (sigla, descricao, categoria) VALUES ('UNX', 'Unidade', 'CONTAGEM') ON CONFLICT DO NOTHING;");
        await _db.Executar("INSERT INTO categorias (categoria) VALUES ('Cat') ON CONFLICT DO NOTHING; INSERT INTO marcas (marca) VALUES ('Marca') ON CONFLICT DO NOTHING;");
        await _db.Executar("INSERT INTO produtos (produto, descricao, categoria_id, marca_id, unidade_medida_id) VALUES ('Prod', 'Desc', (SELECT id FROM categorias LIMIT 1), (SELECT id FROM marcas LIMIT 1), (SELECT id FROM unidades_medida WHERE sigla = 'UNX'));");
        await _db.Executar($"INSERT INTO skus (sku, produto_id, preco, estoque, custo_medio) VALUES ('{Sku}', (SELECT id FROM produtos LIMIT 1), 10, 20, 4);");
        await _db.Executar("INSERT INTO metodos_pagamento (codigo, descricao) VALUES ('99', 'Outros') ON CONFLICT DO NOTHING; INSERT INTO condicoes_pagamentos (descricao, metodo_pagamento_codigo) VALUES ('2x', '99');");

        var emitente = await Escalar<int>("SELECT id FROM emitentes LIMIT 1;");
        var cliente = await Escalar<int>("SELECT id FROM clientes LIMIT 1;");
        var condicao = await Escalar<int>("SELECT id FROM condicoes_pagamentos LIMIT 1;");

        var venda = await Usar<Backend.Core.Common.Results.Resultado<Backend.Core.Features.Vendas.Entities.Venda>, VendasService>(v => v.CriarVenda(new CriarVendaCommand(DateTime.UtcNow, emitente, cliente, null,
            new[] { new CriarVendaItemCommand(Sku, 10, 10, 0) },
            new[] { new CriarVendaPagamentoCommand(condicao, 100, new[] { new CriarParcelaReceberCommand(1, DateTime.UtcNow.AddDays(30), 50), new CriarParcelaReceberCommand(2, DateTime.UtcNow.AddDays(60), 50) }) })));
        Assert.True(venda.Success, venda.Errors?.FirstOrDefault()?.Message);

        if (comNfe)
        {
            var nfe = await Usar<Backend.Core.Common.Results.Resultado<Backend.Core.Features.NFe.Entities.Nfes>, NfesSaidaService>(n => n.EmitirDeVenda(venda.Data!.Id));
            Assert.True(nfe.Success, nfe.Errors?.FirstOrDefault()?.Message);
        }
        return (venda.Data!.Id, await Escalar<int>("SELECT id FROM vendas_itens LIMIT 1;"));
    }

    private async Task<Backend.Core.Features.Vendas.Entities.Devolucao> Devolver(int vendaId, int itemId, decimal quantidade)
    {
        var r = await Usar<Backend.Core.Common.Results.Resultado<Backend.Core.Features.Vendas.Entities.Devolucao>, VendasService>(v =>
            v.CriarDevolucao(vendaId, new CriarDevolucaoCommand("Produto com defeito", new[] { new CriarDevolucaoItemCommand(itemId, quantidade) })));
        Assert.True(r.Success, r.Errors?.FirstOrDefault()?.Message);
        return r.Data!;
    }

    [Fact]
    public async Task Devolucao_de_venda_com_nfe_ativa_emite_nota_de_entrada_e_mantem_a_original()
    {
        var (vendaId, itemId) = await Venda(comNfe: true);
        var chaveOriginal = await Escalar<string>("SELECT chave_acesso FROM nfes WHERE tipo_operacao = 'SAIDA';");

        var devolucao = await Devolver(vendaId, itemId, 6);

        Assert.NotNull(devolucao.NfeDevolucao);
        Assert.Null(devolucao.NfeEstorno);
        Assert.Equal("EMITIDA", await Escalar<string>("SELECT status_nfe::text FROM nfes WHERE tipo_operacao = 'SAIDA';"));

        using var s = _db.NovaSessao();
        var nota = await s.Connection.QuerySingleAsync("SELECT * FROM nfes WHERE finalidade = 'DEVOLUCAO';");
        Assert.Equal(devolucao.NfeDevolucao!.NfeId, (int)nota.id);
        Assert.Equal("ENTRADA", (string)nota.tipo_operacao.ToString());
        Assert.Equal("EMITIDA", (string)nota.status_nfe.ToString());
        Assert.Equal(60m, (decimal)nota.valor_total);
        Assert.Equal(2, (int)nota.numero);
        Assert.Equal(devolucao.Id, (int)nota.devolucao_id);
        Assert.Null((int?)nota.venda_id);
        Assert.Equal(await Escalar<int>("SELECT id FROM nfes WHERE finalidade = 'NORMAL';"), (int)nota.nfe_referenciada_id);
        Assert.Contains($"<refNFe>{chaveOriginal}</refNFe>", (string)nota.xml_documento);
        Assert.Contains("<finNFe>4</finNFe>", (string)nota.xml_documento);
        Assert.Equal(6m, await Escalar<decimal>("SELECT quantidade FROM nfes_itens WHERE nfe_id = " + (int)nota.id + ";"));
    }

    [Fact]
    public async Task Cada_devolucao_emite_a_sua_nota_com_numeracao_seguinte()
    {
        var (vendaId, itemId) = await Venda(comNfe: true);

        await Devolver(vendaId, itemId, 2);
        await Devolver(vendaId, itemId, 3);

        Assert.Equal(new[] { 2, 3 }, (await s_numeros("DEVOLUCAO")));
        Assert.Equal(1, await Escalar<int>("SELECT COUNT(*) FROM nfes WHERE finalidade = 'NORMAL';"));
    }

    private async Task<int[]> s_numeros(string finalidade)
    {
        using var s = _db.NovaSessao();
        return (await s.Connection.QueryAsync<int>($"SELECT numero FROM nfes WHERE finalidade = '{finalidade}' ORDER BY numero;")).ToArray();
    }

    [Fact]
    public async Task Devolucao_de_venda_sem_nfe_ativa_nao_emite_nota()
    {
        var (vendaId, itemId) = await Venda(comNfe: false);

        var devolucao = await Devolver(vendaId, itemId, 6);

        Assert.Null(devolucao.NfeDevolucao);
        Assert.Equal(0, await Escalar<int>("SELECT COUNT(*) FROM nfes;"));
    }

    [Fact]
    public async Task Estorno_da_devolucao_emite_nota_de_saida_referenciando_a_de_devolucao()
    {
        var (vendaId, itemId) = await Venda(comNfe: true);
        var devolucao = await Devolver(vendaId, itemId, 6);

        var r = await Usar<Backend.Core.Common.Results.Resultado<Backend.Core.Features.Vendas.Entities.Devolucao>, VendasService>(v =>
            v.EstornarDevolucao(devolucao.Id, new EstornarDevolucaoCommand("Devolucao lancada errada")));

        Assert.True(r.Success, r.Errors?.FirstOrDefault()?.Message);
        Assert.NotNull(r.Data!.NfeEstorno);
        Assert.Equal(devolucao.NfeDevolucao!.NfeId, r.Data.NfeDevolucao!.NfeId);

        using var s = _db.NovaSessao();
        var nota = await s.Connection.QuerySingleAsync("SELECT * FROM nfes WHERE finalidade = 'ESTORNO_DEVOLUCAO';");
        Assert.Equal("SAIDA", (string)nota.tipo_operacao.ToString());
        Assert.Equal(devolucao.NfeDevolucao.NfeId, (int)nota.nfe_referenciada_id);
        Assert.Equal(3, (int)nota.numero);
        Assert.Equal(60m, (decimal)nota.valor_total);
        Assert.Equal("EMITIDA", await Escalar<string>("SELECT status_nfe::text FROM nfes WHERE finalidade = 'DEVOLUCAO';"));

        var consultada = await Usar<Backend.Core.Features.Vendas.Entities.Devolucao?, VendasService>(v => v.ObterDevolucaoPorId(devolucao.Id));
        Assert.NotNull(consultada!.NfeDevolucao);
        Assert.NotNull(consultada.NfeEstorno);
    }

    [Fact]
    public async Task Estorno_de_devolucao_sem_nota_nao_emite_nota_de_estorno()
    {
        var (vendaId, itemId) = await Venda(comNfe: false);
        var devolucao = await Devolver(vendaId, itemId, 6);

        var r = await Usar<Backend.Core.Common.Results.Resultado<Backend.Core.Features.Vendas.Entities.Devolucao>, VendasService>(v =>
            v.EstornarDevolucao(devolucao.Id, new EstornarDevolucaoCommand("Devolucao lancada errada")));

        Assert.True(r.Success, r.Errors?.FirstOrDefault()?.Message);
        Assert.Null(r.Data!.NfeEstorno);
        Assert.Equal(0, await Escalar<int>("SELECT COUNT(*) FROM nfes;"));
    }
}
