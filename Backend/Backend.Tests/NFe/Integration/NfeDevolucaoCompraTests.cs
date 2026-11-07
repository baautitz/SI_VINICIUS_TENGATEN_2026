using Backend.Core.Common.Results;
using Backend.Core.Features.Compras.Commands;
using Backend.Core.Features.Compras.Entities;
using Backend.Core.Features.Compras.Services;
using Backend.Core.Features.Financeiro.Commands;
using Backend.Infrastructure.PostgreSQL;
using Dapper;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.NFe.Integration;

using Backend.Tests.Common;

[Trait("Category", "Integration")]
[Collection(PostgresCollection.Nome)]
public class NfeDevolucaoCompraTests
{
    private const string Sku = "SKU-NDC";
    private const string ChaveEntrada = "35261045723174000110550010000005001123456780";
    private readonly PostgresFixture _db;
    private readonly IServiceProvider _provider;

    public NfeDevolucaoCompraTests(PostgresFixture db)
    {
        _db = db;
        _provider = new ServiceCollection().AddPostgreSQLInfrastructure(db.ConnectionString).BuildServiceProvider();
        _db.Limpar().GetAwaiter().GetResult();
        _db.Executar("TRUNCATE nfes, devolucoes_compra, compras, emitentes, fornecedores, skus, produtos, movimentacoes_estoque, bairros, cidades, estados RESTART IDENTITY CASCADE;").GetAwaiter().GetResult();
    }

    private async Task<T> Servico<T, TS>(Func<TS, Task<T>> acao) where TS : notnull
    {
        using var escopo = _provider.CreateScope();
        return await acao(escopo.ServiceProvider.GetRequiredService<TS>());
    }

    private async Task<T> Escalar<T>(string sql)
    {
        using var s = _db.NovaSessao();
        return await s.Connection.ExecuteScalarAsync<T>(sql);
    }

    // Compra de 10 un a R$ 10 (R$ 100) em 2 parcelas de 50; com comNfe, registra a NF-e de entrada recebida da compra.
    private async Task<(int CompraId, int ItemId)> Compra(bool comNfe)
    {
        await _db.Executar("INSERT INTO estados (pais_id, estado, uf) VALUES ((SELECT id FROM paises LIMIT 1), 'Sao Paulo', 'SP');");
        await _db.Executar("INSERT INTO cidades (cidade, ddd, estado_id) VALUES ('Sao Paulo', '11', (SELECT id FROM estados LIMIT 1));");
        await _db.Executar("INSERT INTO bairros (bairro, cidade_id) VALUES ('Centro', (SELECT id FROM cidades LIMIT 1));");
        await _db.Executar("INSERT INTO emitentes (tipo_pessoa, nome_razaosocial, cpf_cnpj, nacionalidade_id, bairro_id) VALUES ('JURIDICA', 'Emitente', '45723174000110', (SELECT id FROM paises LIMIT 1), (SELECT id FROM bairros LIMIT 1));");
        await _db.Executar("INSERT INTO fornecedores (tipo_pessoa, nome_razaosocial, cpf_cnpj, nacionalidade_id) VALUES ('JURIDICA', 'Fornecedor Z', '11222333000181', (SELECT id FROM paises LIMIT 1));");
        await _db.Executar("INSERT INTO unidades_medida (sigla, descricao, categoria) VALUES ('UNX', 'Unidade', 'CONTAGEM') ON CONFLICT DO NOTHING;");
        await _db.Executar("INSERT INTO categorias (categoria) VALUES ('Cat') ON CONFLICT DO NOTHING; INSERT INTO marcas (marca) VALUES ('Marca') ON CONFLICT DO NOTHING;");
        await _db.Executar("INSERT INTO produtos (produto, descricao, categoria_id, marca_id, unidade_medida_id) VALUES ('Prod', 'Desc', (SELECT id FROM categorias LIMIT 1), (SELECT id FROM marcas LIMIT 1), (SELECT id FROM unidades_medida WHERE sigla = 'UNX'));");
        await _db.Executar($"INSERT INTO skus (sku, produto_id, preco, estoque, custo_medio) VALUES ('{Sku}', (SELECT id FROM produtos LIMIT 1), 20, 0, 0);");

        var emitente = await Escalar<int>("SELECT id FROM emitentes LIMIT 1;");
        var fornecedor = await Escalar<int>("SELECT id FROM fornecedores LIMIT 1;");

        var compra = await Servico<Resultado<Compra>, ComprasService>(c => c.CriarCompra(new CriarCompraCommand(
            DateTime.UtcNow, emitente, fornecedor, 0, 0, 0, 0, 0, null, null, null,
            new[] { new CriarCompraItemCommand(Sku, 10, 10, 0, null) }, null,
            new[] { new CriarParcelaPagarCommand(1, DateTime.UtcNow.Date.AddDays(30), 50), new CriarParcelaPagarCommand(2, DateTime.UtcNow.Date.AddDays(60), 50) })));
        Assert.True(compra.Success, compra.Errors?.FirstOrDefault()?.Message);

        if (comNfe)
            await _db.Executar(@"INSERT INTO nfes (chave_acesso, numero, serie, data_emissao, emitente_id, emitente_nome_razaosocial, emitente_cpf_cnpj,
                                    fornecedor_id, cliente_nome_razaosocial, cliente_cpf_cnpj, tipo_operacao, status_nfe, compra_id, valor_produtos, valor_total)
                                 VALUES (@Chave, 500, 1, now(), @Emitente, 'Emitente', '45723174000110', @Fornecedor, 'Fornecedor Z', '11222333000181',
                                    'ENTRADA', 'EMITIDA', @Compra, 100, 100);",
                new { Chave = ChaveEntrada, Emitente = emitente, Fornecedor = fornecedor, Compra = compra.Data!.Id });

        return (compra.Data!.Id, await Escalar<int>("SELECT id FROM compras_itens LIMIT 1;"));
    }

    private async Task<DevolucaoCompra> Devolver(int compraId, int itemId, decimal quantidade)
    {
        var r = await Servico<Resultado<DevolucaoCompra>, DevolucoesCompraService>(
            s => s.CriarDevolucao(compraId, new CriarDevolucaoCompraCommand("Produto com defeito", new[] { new CriarDevolucaoCompraItemCommand(itemId, quantidade) })));
        Assert.True(r.Success, r.Errors?.FirstOrDefault()?.Message);
        return r.Data!;
    }

    [Fact]
    public async Task Devolucao_de_compra_com_nfe_ativa_emite_nota_de_saida_e_mantem_a_original()
    {
        var (compraId, itemId) = await Compra(comNfe: true);

        var devolucao = await Devolver(compraId, itemId, 6);

        Assert.NotNull(devolucao.NfeDevolucao);
        Assert.Null(devolucao.NfeEstorno);
        Assert.Equal("EMITIDA", await Escalar<string>("SELECT status_nfe::text FROM nfes WHERE finalidade = 'NORMAL';"));

        using var s = _db.NovaSessao();
        var nota = await s.Connection.QuerySingleAsync("SELECT * FROM nfes WHERE finalidade = 'DEVOLUCAO';");
        Assert.Equal(devolucao.NfeDevolucao!.NfeId, (int)nota.id);
        Assert.Equal("SAIDA", (string)nota.tipo_operacao.ToString());
        Assert.Equal("EMITIDA", (string)nota.status_nfe.ToString());
        Assert.Equal(60m, (decimal)nota.valor_total);
        Assert.Equal(1, (int)nota.numero);
        Assert.Equal(devolucao.Id, (int)nota.devolucao_compra_id);
        Assert.Null((int?)nota.compra_id);
        Assert.Null((int?)nota.devolucao_id);
        Assert.Equal(await Escalar<int>("SELECT id FROM nfes WHERE finalidade = 'NORMAL';"), (int)nota.nfe_referenciada_id);
        Assert.Equal(await Escalar<int>("SELECT id FROM fornecedores LIMIT 1;"), (int)nota.fornecedor_id);
        Assert.Contains($"<refNFe>{ChaveEntrada}</refNFe>", (string)nota.xml_documento);
        Assert.Contains("<CFOP>5202</CFOP>", (string)nota.xml_documento);
        Assert.Equal(6m, await Escalar<decimal>("SELECT quantidade FROM nfes_itens WHERE nfe_id = " + (int)nota.id + ";"));
    }

    [Fact]
    public async Task Devolucao_de_compra_sem_nfe_ativa_nao_emite_nota()
    {
        var (compraId, itemId) = await Compra(comNfe: false);

        var devolucao = await Devolver(compraId, itemId, 6);

        Assert.Null(devolucao.NfeDevolucao);
        Assert.Equal(0, await Escalar<int>("SELECT COUNT(*) FROM nfes;"));
    }

    [Fact]
    public async Task Estorno_da_devolucao_emite_nota_de_entrada_referenciando_a_de_devolucao()
    {
        var (compraId, itemId) = await Compra(comNfe: true);
        var devolucao = await Devolver(compraId, itemId, 6);

        var r = await Servico<Resultado<DevolucaoCompra>, DevolucoesCompraService>(
            s => s.EstornarDevolucao(devolucao.Id, new EstornarDevolucaoCompraCommand("Devolucao lancada errada")));

        Assert.True(r.Success, r.Errors?.FirstOrDefault()?.Message);
        Assert.NotNull(r.Data!.NfeEstorno);

        using var s = _db.NovaSessao();
        var nota = await s.Connection.QuerySingleAsync("SELECT * FROM nfes WHERE finalidade = 'ESTORNO_DEVOLUCAO';");
        Assert.Equal("ENTRADA", (string)nota.tipo_operacao.ToString());
        Assert.Equal(devolucao.NfeDevolucao!.NfeId, (int)nota.nfe_referenciada_id);
        Assert.Equal(2, (int)nota.numero);
        Assert.Equal(60m, (decimal)nota.valor_total);
        Assert.Equal("EMITIDA", await Escalar<string>("SELECT status_nfe::text FROM nfes WHERE finalidade = 'DEVOLUCAO';"));

        var consultada = await Servico<DevolucaoCompra?, DevolucoesCompraService>(c => c.ObterDevolucaoPorId(devolucao.Id));
        Assert.NotNull(consultada!.NfeDevolucao);
        Assert.NotNull(consultada.NfeEstorno);
    }

    [Fact]
    public async Task Estorno_de_devolucao_sem_nota_nao_emite_nota_de_estorno()
    {
        var (compraId, itemId) = await Compra(comNfe: false);
        var devolucao = await Devolver(compraId, itemId, 6);

        var r = await Servico<Resultado<DevolucaoCompra>, DevolucoesCompraService>(
            s => s.EstornarDevolucao(devolucao.Id, new EstornarDevolucaoCompraCommand("Devolucao lancada errada")));

        Assert.True(r.Success, r.Errors?.FirstOrDefault()?.Message);
        Assert.Null(r.Data!.NfeEstorno);
        Assert.Equal(0, await Escalar<int>("SELECT COUNT(*) FROM nfes;"));
    }
}
