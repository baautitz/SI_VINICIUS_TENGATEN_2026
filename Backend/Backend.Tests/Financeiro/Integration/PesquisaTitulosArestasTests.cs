using Backend.Infrastructure.PostgreSQL.Features.Financeiro;
using Backend.Tests.Common;
using Dapper;

namespace Backend.Tests.Financeiro.Integration;

// Arestas da pesquisa de títulos (#38): curingas, rótulos em português, datas e valor.
[Trait("Category", "Integration")]
[Collection(PostgresCollection.Nome)]
public class PesquisaTitulosArestasTests
{
    private readonly PostgresFixture _db;

    public PesquisaTitulosArestasTests(PostgresFixture db)
    {
        _db = db;
        _db.Limpar().GetAwaiter().GetResult();
    }

    private async Task<int> Cliente()
    {
        using var s = _db.NovaSessao();
        return await s.Connection.ExecuteScalarAsync<int>(
            "INSERT INTO clientes (tipo_pessoa, nome_razaosocial, cpf_cnpj, nacionalidade_id) VALUES ('FISICA', 'Maria Cliente', '52998224725', (SELECT id FROM paises LIMIT 1)) RETURNING id;");
    }

    private Task Receber(int cliente, string documento, string descricao, decimal valor = 5m, string vencimento = "2026-02-05",
        string status = "ABERTO", string origem = "MANUAL") =>
        _db.Executar(@"INSERT INTO contas_receber (origem_tipo, origem_id, numero_documento, numero_parcela, total_parcelas, cliente_id, descricao, data_vencimento, valor, status)
            VALUES (@origem::origem_titulo_financeiro_enum, CASE WHEN @origem = 'MANUAL' THEN NULL ELSE 1 END, @documento, 1, 1, @cliente, @descricao, @vencimento::date, @valor, @status::status_titulo_financeiro_enum);",
            new { origem, documento, cliente, descricao, vencimento, valor, status });

    private async Task<string[]> Documentos(string termo)
    {
        using var s = _db.NovaSessao();
        return (await new ContasReceberRepository(s).PesquisarContasReceber(termo)).Itens.Select(c => c.NumeroDocumento).Order().ToArray();
    }

    [Theory]
    [InlineData("%")]
    [InlineData("_")]
    [InlineData("al%el")]
    [InlineData("a_uguel")]
    public async Task Curingas_digitados_sao_texto_literal(string termo)
    {
        var c = await Cliente();
        await Receber(c, "L-1", "Aluguel do galpao");
        await Receber(c, "L-2", "Desconto de 10% e item_1 al%el a_uguel");

        Assert.Equal(["L-2"], await Documentos(termo));
    }

    [Theory]
    [InlineData("pago", "PAGO", "MANUAL")]
    [InlineData("aberto", "ABERTO", "MANUAL")]
    [InlineData("parcial", "PARCIAL", "MANUAL")]
    [InlineData("cancelado", "CANCELADO", "MANUAL")]
    [InlineData("venda", "ABERTO", "VENDA")]
    public async Task Rotulo_em_portugues_encontra_pelo_status_e_pela_origem(string termo, string status, string origem)
    {
        var c = await Cliente();
        await Receber(c, "ALVO", "Titulo", status: status, origem: origem);
        await Receber(c, "OUTRO", "Titulo", status: status == "ABERTO" ? "CANCELADO" : "ABERTO", origem: "MANUAL");

        Assert.Equal(["ALVO"], await Documentos(termo));
    }

    [Fact]
    public async Task Origem_composta_e_pesquisada_pelo_rotulo_e_nao_pelo_nome_interno()
    {
        var c = await Cliente();
        await _db.Executar("INSERT INTO contas_pagar (origem_tipo, origem_id, numero_documento, numero_parcela, total_parcelas, cliente_id, descricao, data_vencimento, valor) VALUES ('DEVOLUCAO_VENDA', 1, 'DV-1', 1, 1, @c, 'Reembolso', '2026-02-05', 5);", new { c });

        using var s = _db.NovaSessao();
        var repo = new ContasPagarRepository(s);
        Assert.Single((await repo.PesquisarContasPagar("devolução venda")).Itens);
        Assert.Empty((await repo.PesquisarContasPagar("DEVOLUCAO_VENDA")).Itens);
    }

    [Theory]
    [InlineData("15/03/2026", new[] { "D-1" })]
    [InlineData("03/2026", new[] { "D-1", "D-2" })]
    [InlineData("20/04/2026", new[] { "D-3" })]
    public async Task Data_em_dd_mm_aaaa_ou_mes_ano_e_encontrada(string termo, string[] esperado)
    {
        var c = await Cliente();
        await Receber(c, "D-1", "Titulo", vencimento: "2026-03-15");
        await Receber(c, "D-2", "Titulo", vencimento: "2026-03-28");
        await Receber(c, "D-3", "Titulo", vencimento: "2026-04-20");

        Assert.Equal(esperado, await Documentos(termo));
    }

    [Theory]
    [InlineData("10", new[] { "VA" })]
    [InlineData("1100", new[] { "VB" })]
    [InlineData("1.100,00", new[] { "VB" })]
    [InlineData("210,00", new[] { "VC" })]
    public async Task Valor_so_casa_por_valor_inteiro_e_nao_por_trecho_de_digitos(string termo, string[] esperado)
    {
        var c = await Cliente();
        await Receber(c, "VA", "Titulo", valor: 10m);
        await Receber(c, "VB", "Titulo", valor: 1100m);
        await Receber(c, "VC", "Titulo", valor: 210m);

        Assert.Equal(esperado, await Documentos(termo));
    }
}
