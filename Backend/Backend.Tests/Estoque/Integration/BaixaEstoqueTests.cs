using Backend.Core.Common.Results;
using Backend.Core.Features.Estoque.Commands;
using Backend.Core.Features.Estoque.Entities;
using Backend.Core.Features.Estoque.Services;
using Backend.Infrastructure.PostgreSQL;
using Dapper;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.Estoque.Integration;

using Backend.Tests.Common;

// ADR 0006: saída manual com motivo tipado, NF-e de baixa (CFOP 5927), estorno da baixa (CFOP 1949)
// e fechamento do balanço com falta justificada e sobra recontada. Serviços reais sobre o schema real.
[Trait("Category", "Integration")]
[Collection(PostgresCollection.Nome)]
public class BaixaEstoqueTests
{
    private const string Sku = "SKU-BX";
    private readonly PostgresFixture _db;
    private readonly IServiceProvider _provider;

    public BaixaEstoqueTests(PostgresFixture db)
    {
        _db = db;
        _provider = new ServiceCollection().AddPostgreSQLInfrastructure(db.ConnectionString).BuildServiceProvider();
        _db.Executar("TRUNCATE nfes, balancos, movimentacoes_estoque, emitentes, skus, produtos, bairros, cidades, estados RESTART IDENTITY CASCADE;").GetAwaiter().GetResult();
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

    private Task<decimal> Estoque() => Escalar<decimal>($"SELECT estoque FROM skus WHERE sku = '{Sku}';");

    // Emitente com bairro/cidade/estado (a chave de acesso usa a UF) e um SKU com 10 un a custo médio 5,00.
    private async Task<int> Cenario()
    {
        await _db.Executar("INSERT INTO estados (pais_id, estado, uf) VALUES ((SELECT id FROM paises LIMIT 1), 'Sao Paulo', 'SP');");
        await _db.Executar("INSERT INTO cidades (cidade, ddd, estado_id) VALUES ('Sao Paulo', '11', (SELECT id FROM estados LIMIT 1));");
        await _db.Executar("INSERT INTO bairros (bairro, cidade_id) VALUES ('Centro', (SELECT id FROM cidades LIMIT 1));");
        await _db.Executar("INSERT INTO emitentes (tipo_pessoa, nome_razaosocial, cpf_cnpj, nacionalidade_id, bairro_id) VALUES ('JURIDICA', 'Emitente', '45723174000110', (SELECT id FROM paises LIMIT 1), (SELECT id FROM bairros LIMIT 1));");
        await _db.Executar("INSERT INTO unidades_medida (sigla, descricao, categoria) VALUES ('UNX', 'Unidade', 'CONTAGEM') ON CONFLICT DO NOTHING;");
        await _db.Executar("INSERT INTO categorias (categoria) VALUES ('Cat') ON CONFLICT DO NOTHING; INSERT INTO marcas (marca) VALUES ('Marca') ON CONFLICT DO NOTHING;");
        await _db.Executar("INSERT INTO produtos (produto, descricao, categoria_id, marca_id, unidade_medida_id) VALUES ('Prod', 'Desc', (SELECT id FROM categorias LIMIT 1), (SELECT id FROM marcas LIMIT 1), (SELECT id FROM unidades_medida WHERE sigla = 'UNX'));");
        await _db.Executar($"INSERT INTO skus (sku, produto_id, preco, estoque, custo_medio) VALUES ('{Sku}', (SELECT id FROM produtos LIMIT 1), 20, 10, 5);");
        return await Escalar<int>("SELECT id FROM emitentes LIMIT 1;");
    }

    private Task<Resultado<MovimentacoesEstoques>> Saida(string motivoTipo, decimal quantidade, int? emitenteId)
        => Servico<Resultado<MovimentacoesEstoques>, MovimentacoesEstoquesService>(s => s.CriarMovimentacao(
            new CriarMovimentacaoCommand(motivoTipo, null, "Detalhe do motivo", emitenteId,
                new List<MovimentacaoItemCommand> { new(Sku, quantidade, null) })));

    // ---- Movimentação manual -------------------------------------------------------------------------

    [Fact]
    public async Task Saida_por_perda_emite_nfe_de_baixa_5927_vinculada_a_movimentacao()
    {
        var emitente = await Cenario();

        var r = await Saida("PERDA_AVARIA", 3, emitente);

        Assert.True(r.Success, r.Errors?.FirstOrDefault()?.Message);
        Assert.Equal(7m, await Estoque());
        using var s = _db.NovaSessao();
        var nota = await s.Connection.QuerySingleAsync("SELECT * FROM nfes;");
        Assert.Equal("BAIXA_ESTOQUE", (string)nota.finalidade.ToString());
        Assert.Equal("SAIDA", (string)nota.tipo_operacao.ToString());
        Assert.Equal("EMITIDA", (string)nota.status_nfe.ToString());
        Assert.Equal(r.Data!.Id, (int)nota.movimentacao_estoque_id);
        Assert.Equal(15m, (decimal)nota.valor_total);
        Assert.Null((int?)nota.cliente_id);
        Assert.Null((int?)nota.fornecedor_id);
        Assert.Null((int?)nota.nfe_referenciada_id);
        Assert.Equal("45723174000110", (string)nota.cliente_cpf_cnpj);
        Assert.Contains("<CFOP>5927</CFOP>", (string)nota.xml_documento);
        Assert.DoesNotContain("<NFref>", (string)nota.xml_documento);
        Assert.Equal("PERDA_AVARIA", await Escalar<string>("SELECT motivo_tipo::text FROM movimentacoes_estoque;"));
    }

    [Theory]
    [InlineData("FURTO_ROUBO")]
    [InlineData("VENCIMENTO")]
    public async Task Furto_e_vencimento_tambem_emitem_nfe_de_baixa(string motivo)
    {
        var emitente = await Cenario();

        Assert.True((await Saida(motivo, 1, emitente)).Success);

        Assert.Equal(1, await Escalar<int>("SELECT COUNT(*) FROM nfes WHERE finalidade = 'BAIXA_ESTOQUE';"));
    }

    [Theory]
    [InlineData("OUTROS")]
    [InlineData("ERRO_DE_CONTAGEM")]
    public async Task Saida_manual_sem_nota_e_recusada_e_nao_mexe_no_estoque(string motivo)
    {
        await Cenario();

        var r = await Saida(motivo, 2, null);

        Assert.False(r.Success);
        Assert.Contains(r.Errors!, e => e.Code == "MOTIVO_TIPO_INVALIDO");
        Assert.Equal(10m, await Estoque());
        Assert.Equal(0, await Escalar<int>("SELECT COUNT(*) FROM nfes;"));
    }

    [Fact]
    public async Task Saida_por_perda_sem_emitente_e_recusada_e_nao_mexe_no_estoque()
    {
        await Cenario();

        var r = await Saida("PERDA_AVARIA", 3, null);

        Assert.False(r.Success);
        Assert.Contains(r.Errors!, e => e.Code == "EMITENTE_OBRIGATORIO");
        Assert.Equal(10m, await Estoque());
    }

    [Theory]
    [InlineData("ERRO_DE_CONTAGEM")]
    [InlineData("INEXISTENTE")]
    [InlineData("")]
    public async Task Saida_manual_so_aceita_motivo_da_lista(string motivo)
    {
        var emitente = await Cenario();

        var r = await Saida(motivo, 1, emitente);

        Assert.False(r.Success);
        Assert.Contains(r.Errors!, e => e.Code is "MOTIVO_TIPO_INVALIDO" or "MOTIVO_TIPO_OBRIGATORIO");
        Assert.Equal(10m, await Estoque());
    }

    [Fact]
    public async Task Banco_recusa_entrada_manual()
    {
        await Cenario();

        await Assert.ThrowsAnyAsync<Exception>(() => _db.Executar(
            "INSERT INTO movimentacoes_estoque (tipo_movimentacao, origem_tipo, motivo, motivo_tipo) VALUES ('ENTRADA', 'MANUAL', 'Entrada sem compra', 'OUTROS');"));
    }

    [Fact]
    public async Task Estorno_da_baixa_emite_nfe_de_estorno_1949_referenciando_a_baixa_e_devolve_o_saldo()
    {
        var emitente = await Cenario();
        var baixa = await Saida("PERDA_AVARIA", 3, emitente);

        var r = await Servico<Resultado<MovimentacoesEstoques>, MovimentacoesEstoquesService>(s => s.Estornar(baixa.Data!.Id, new EstornarMovimentacaoCommand("Contagem errada na prateleira")));

        Assert.True(r.Success, r.Errors?.FirstOrDefault()?.Message);
        Assert.Equal(10m, await Estoque());
        using var s = _db.NovaSessao();
        var notaBaixa = await s.Connection.QuerySingleAsync("SELECT * FROM nfes WHERE finalidade = 'BAIXA_ESTOQUE';");
        var estorno = await s.Connection.QuerySingleAsync("SELECT * FROM nfes WHERE finalidade = 'ESTORNO_BAIXA_ESTOQUE';");
        Assert.Equal("ENTRADA", (string)estorno.tipo_operacao.ToString());
        Assert.Equal((int)notaBaixa.id, (int)estorno.nfe_referenciada_id);
        Assert.Equal(baixa.Data!.Id, (int)estorno.movimentacao_estoque_id);
        Assert.Contains("<CFOP>1949</CFOP>", (string)estorno.xml_documento);
        Assert.Contains($"<refNFe>{(string)notaBaixa.chave_acesso}</refNFe>", (string)estorno.xml_documento);
        Assert.Equal("EMITIDA", (string)notaBaixa.status_nfe.ToString());
    }

    // ---- Balanço -------------------------------------------------------------------------------------

    private async Task<int> Balanco(decimal contada, string? motivo = null, bool recontado = false)
    {
        var r = await Servico<Resultado<Balancos>, BalancosService>(s => s.CriarBalanco(new CriarBalancoCommand(null, null,
            new List<ContagemItemCommand> { new(Sku, contada, motivo, recontado) })));
        Assert.True(r.Success, r.Errors?.FirstOrDefault()?.Message);
        return r.Data!.Id;
    }

    private Task<Resultado<Balancos>> Fechar(int id, int? emitenteId = null)
        => Servico<Resultado<Balancos>, BalancosService>(s => s.Fechar(id, new FecharBalancoCommand(emitenteId)));

    [Fact]
    public async Task Falta_sem_motivo_nao_fecha_o_balanco()
    {
        var emitente = await Cenario();
        var id = await Balanco(contada: 8);

        var r = await Fechar(id, emitente);

        Assert.False(r.Success);
        Assert.Contains(r.Errors!, e => e.Code == "MOTIVO_DIVERGENCIA_OBRIGATORIO");
        Assert.Equal(10m, await Estoque());
        Assert.Equal("ABERTO", await Escalar<string>($"SELECT status::text FROM balancos WHERE id = {id};"));
    }

    [Fact]
    public async Task Falta_por_perda_fecha_o_balanco_e_emite_nfe_de_baixa()
    {
        var emitente = await Cenario();
        var id = await Balanco(contada: 8, motivo: "PERDA_AVARIA");

        var r = await Fechar(id, emitente);

        Assert.True(r.Success, r.Errors?.FirstOrDefault()?.Message);
        Assert.Equal(8m, await Estoque());
        using var s = _db.NovaSessao();
        var mov = await s.Connection.QuerySingleAsync("SELECT * FROM movimentacoes_estoque;");
        Assert.Equal("BALANCO", (string)mov.origem_tipo.ToString());
        Assert.Equal("SAIDA", (string)mov.tipo_movimentacao.ToString());
        Assert.Equal("PERDA_AVARIA", (string)mov.motivo_tipo.ToString());
        var nota = await s.Connection.QuerySingleAsync("SELECT * FROM nfes;");
        Assert.Equal("BAIXA_ESTOQUE", (string)nota.finalidade.ToString());
        Assert.Equal((int)mov.id, (int)nota.movimentacao_estoque_id);
        Assert.Equal(10m, (decimal)nota.valor_total);
    }

    [Fact]
    public async Task Falta_por_perda_sem_emitente_nao_fecha_o_balanco()
    {
        await Cenario();
        var id = await Balanco(contada: 8, motivo: "FURTO_ROUBO");

        var r = await Fechar(id);

        Assert.False(r.Success);
        Assert.Contains(r.Errors!, e => e.Code == "EMITENTE_OBRIGATORIO");
        Assert.Equal(10m, await Estoque());
        Assert.Equal(0, await Escalar<int>("SELECT COUNT(*) FROM movimentacoes_estoque;"));
    }

    [Fact]
    public async Task Falta_por_erro_de_contagem_e_ajuste_interno_sem_nota_nem_emitente()
    {
        await Cenario();
        var id = await Balanco(contada: 9, motivo: "ERRO_DE_CONTAGEM");

        var r = await Fechar(id);

        Assert.True(r.Success, r.Errors?.FirstOrDefault()?.Message);
        Assert.Equal(9m, await Estoque());
        Assert.Equal(0, await Escalar<int>("SELECT COUNT(*) FROM nfes;"));
    }

    [Fact]
    public async Task Falta_com_motivo_outros_e_recusada()
    {
        var emitente = await Cenario();
        var id = await Balanco(contada: 9, motivo: "OUTROS");

        var r = await Fechar(id, emitente);

        Assert.Contains(r.Errors!, e => e.Code == "MOTIVO_FALTA_INVALIDO");
    }

    [Fact]
    public async Task Sobra_sem_recontagem_nao_fecha_o_balanco()
    {
        await Cenario();
        var id = await Balanco(contada: 12, motivo: "ERRO_DE_CONTAGEM", recontado: false);

        var r = await Fechar(id);

        Assert.False(r.Success);
        Assert.Contains(r.Errors!, e => e.Code == "RECONTAGEM_OBRIGATORIA");
        Assert.Equal(10m, await Estoque());
    }

    [Fact]
    public async Task Sobra_com_motivo_de_perda_e_recusada()
    {
        var emitente = await Cenario();
        var id = await Balanco(contada: 12, motivo: "PERDA_AVARIA", recontado: true);

        var r = await Fechar(id, emitente);

        Assert.Contains(r.Errors!, e => e.Code == "MOTIVO_SOBRA_INVALIDO");
    }

    [Fact]
    public async Task Sobra_recontada_entra_no_estoque_sem_emitir_nota()
    {
        await Cenario();
        var id = await Balanco(contada: 12, motivo: "ERRO_DE_CONTAGEM", recontado: true);

        var r = await Fechar(id);

        Assert.True(r.Success, r.Errors?.FirstOrDefault()?.Message);
        Assert.Equal(12m, await Estoque());
        Assert.Equal("ENTRADA", await Escalar<string>("SELECT tipo_movimentacao::text FROM movimentacoes_estoque;"));
        Assert.Equal(0, await Escalar<int>("SELECT COUNT(*) FROM nfes;"));
    }

    [Fact]
    public async Task Item_sem_divergencia_fecha_sem_motivo()
    {
        await Cenario();
        var id = await Balanco(contada: 10);

        var r = await Fechar(id);

        Assert.True(r.Success, r.Errors?.FirstOrDefault()?.Message);
        Assert.Equal(0, await Escalar<int>("SELECT COUNT(*) FROM movimentacoes_estoque;"));
    }

    [Fact]
    public async Task Cancelar_balanco_fechado_com_baixa_emite_nfe_de_estorno_da_baixa()
    {
        var emitente = await Cenario();
        var id = await Balanco(contada: 8, motivo: "VENCIMENTO");
        Assert.True((await Fechar(id, emitente)).Success);

        var r = await Servico<Resultado<Balancos>, BalancosService>(s => s.Cancelar(id, new CancelarBalancoCommand("Balanço feito no dia errado")));

        Assert.True(r.Success, r.Errors?.FirstOrDefault()?.Message);
        Assert.Equal(10m, await Estoque());
        Assert.Equal(1, await Escalar<int>("SELECT COUNT(*) FROM nfes WHERE finalidade = 'ESTORNO_BAIXA_ESTOQUE';"));
    }
}
