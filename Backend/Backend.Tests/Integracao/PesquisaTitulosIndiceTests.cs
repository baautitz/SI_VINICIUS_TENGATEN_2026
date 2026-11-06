using Backend.Infrastructure.PostgreSQL.Common;
using Backend.Infrastructure.PostgreSQL.Features.Financeiro;
using Backend.Tests.Common;
using Dapper;

namespace Backend.Tests.Integracao;

[Collection(PostgresCollection.Nome)]
public class PesquisaTitulosIndiceTests
{
    private readonly PostgresFixture _db;

    public PesquisaTitulosIndiceTests(PostgresFixture db)
    {
        _db = db;
        _db.Limpar().GetAwaiter().GetResult();
    }

    private async Task<int> Fornecedor(string nome)
    {
        using var s = _db.NovaSessao();
        return await s.Connection.ExecuteScalarAsync<int>(
            "INSERT INTO fornecedores (tipo_pessoa, nome_razaosocial, cpf_cnpj, nacionalidade_id) VALUES ('JURIDICA', @nome, '11222333000181', (SELECT id FROM paises LIMIT 1)) RETURNING id;",
            new { nome });
    }

    private async Task<int> Cliente(string nome)
    {
        using var s = _db.NovaSessao();
        return await s.Connection.ExecuteScalarAsync<int>(
            "INSERT INTO clientes (tipo_pessoa, nome_razaosocial, cpf_cnpj, nacionalidade_id) VALUES ('FISICA', @nome, '52998224725', (SELECT id FROM paises LIMIT 1)) RETURNING id;",
            new { nome });
    }

    [Theory]
    [InlineData("contas_pagar", "cp", "fornecedor_id")]
    [InlineData("contas_receber", "cr", "cliente_id")]
    public async Task Pesquisa_usa_indice_trigram_em_tabela_com_volume_realista(string tabela, string alias, string coluna)
    {
        var parceiro = coluna == "fornecedor_id" ? await Fornecedor("Distribuidora Aurora") : await Cliente("Maria Cliente");
        await _db.Executar($@"
            INSERT INTO {tabela} (numero_documento, numero_parcela, total_parcelas, {coluna}, descricao, data_vencimento, valor)
            SELECT 'DOC-' || g, 1, 1, @parceiro, 'Titulo numero ' || md5(g::text), CURRENT_DATE + (g % 365), g
            FROM generate_series(1, 100000) g;
            INSERT INTO {tabela} (numero_documento, numero_parcela, total_parcelas, {coluna}, descricao, data_vencimento, valor)
            VALUES ('ALVO-1', 1, 1, @parceiro, 'Aluguel do galpao', CURRENT_DATE, 10);
            ANALYZE {tabela};", new { parceiro });

        var param = PesquisaTitulos.Parametros("aluguel", null, 1, 20);
        var sql = $"EXPLAIN SELECT {alias}.id FROM {tabela} {alias} " + PesquisaTitulos.Where($"{alias}.texto_pesquisa", PesquisaTitulos.Analisar("aluguel"));

        using var s = _db.NovaSessao();
        await PesquisaTitulos.AjustarLimiar(s.Connection, null);
        var plano = string.Join("\n", await s.Connection.QueryAsync<string>(sql, param));

        Assert.True(plano.Contains("Bitmap Index Scan"), plano);
        Assert.False(plano.Contains("Seq Scan"), plano);
    }

    [Fact]
    public async Task Texto_pesquisavel_acompanha_a_edicao_do_titulo_e_do_fornecedor()
    {
        var f = await Fornecedor("Distribuidora Aurora");
        await _db.Executar("INSERT INTO contas_pagar (numero_documento, numero_parcela, total_parcelas, fornecedor_id, descricao, data_vencimento, valor) VALUES ('ED-1', 1, 1, @f, 'Aluguel', CURRENT_DATE, 100);", new { f });

        using var s = _db.NovaSessao();
        var repo = new ContasPagarRepository(s);
        Assert.Single((await repo.PesquisarContasPagar("aurora")).Itens);

        await _db.Executar("UPDATE fornecedores SET nome_razaosocial = 'Comercial Zenite' WHERE id = @f;", new { f });
        Assert.Empty((await repo.PesquisarContasPagar("aurora")).Itens);
        Assert.Single((await repo.PesquisarContasPagar("zenite")).Itens);

        await _db.Executar("UPDATE contas_pagar SET descricao = 'Energia' WHERE fornecedor_id = @f;", new { f });
        Assert.Empty((await repo.PesquisarContasPagar("aluguel")).Itens);
        Assert.Single((await repo.PesquisarContasPagar("energia")).Itens);
    }

    [Fact]
    public async Task Texto_pesquisavel_do_receber_acompanha_a_edicao_do_cliente()
    {
        var c = await Cliente("Maria Cliente");
        await _db.Executar("INSERT INTO contas_receber (numero_documento, numero_parcela, total_parcelas, cliente_id, descricao, data_vencimento, valor) VALUES ('ED-2', 1, 1, @c, 'Venda', CURRENT_DATE, 100);", new { c });

        using var s = _db.NovaSessao();
        var repo = new ContasReceberRepository(s);
        Assert.Single((await repo.PesquisarContasReceber("maria")).Itens);

        await _db.Executar("UPDATE clientes SET nome_razaosocial = 'Joana Cliente' WHERE id = @c;", new { c });
        Assert.Empty((await repo.PesquisarContasReceber("maria")).Itens);
        Assert.Single((await repo.PesquisarContasReceber("joana")).Itens);
    }
}
