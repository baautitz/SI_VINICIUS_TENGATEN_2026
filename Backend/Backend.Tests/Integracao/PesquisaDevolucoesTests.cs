using Backend.Infrastructure.PostgreSQL.Features.Vendas;
using Dapper;

namespace Backend.Tests.Integracao;

[Collection(PostgresCollection.Nome)]
public class PesquisaDevolucoesTests
{
    private readonly PostgresFixture _db;

    public PesquisaDevolucoesTests(PostgresFixture db)
    {
        _db = db;
        _db.Limpar().GetAwaiter().GetResult();
        _db.Executar("TRUNCATE devolucoes, vendas, emitentes RESTART IDENTITY CASCADE;").GetAwaiter().GetResult();
        _db.Executar("INSERT INTO emitentes (tipo_pessoa, nome_razaosocial, cpf_cnpj, nacionalidade_id) VALUES ('JURIDICA', 'Emitente', '45723174000110', (SELECT id FROM paises LIMIT 1));").GetAwaiter().GetResult();
    }

    private static readonly string[] _cpfs = { "52998224725", "11144477735" };
    private int _proximoCpf;

    private async Task<int> Venda(string cliente)
    {
        using var s = _db.NovaSessao();
        var clienteId = await s.Connection.ExecuteScalarAsync<int>(
            "INSERT INTO clientes (tipo_pessoa, nome_razaosocial, cpf_cnpj, nacionalidade_id) VALUES ('FISICA', @cliente, @cpf, (SELECT id FROM paises LIMIT 1)) RETURNING id;",
            new { cliente, cpf = _cpfs[_proximoCpf++ % _cpfs.Length] });
        var emitenteId = await s.Connection.ExecuteScalarAsync<int>(
            "SELECT id FROM emitentes LIMIT 1;");
        return await s.Connection.ExecuteScalarAsync<int>(
            "INSERT INTO vendas (emitente_id, cliente_id, valor_total) VALUES (@emitenteId, @clienteId, 100) RETURNING id;",
            new { emitenteId, clienteId });
    }

    private Task Devolucao(int vendaId, string motivo, int diasAtras = 0) =>
        _db.Executar("INSERT INTO devolucoes (venda_id, data_devolucao, motivo, valor_total) VALUES (@vendaId, NOW() - make_interval(days => @diasAtras), @motivo, 10);",
            new { vendaId, motivo, diasAtras });

    [Fact]
    public async Task Filtra_por_venda_e_traz_nome_do_cliente()
    {
        var v1 = await Venda("Maria Silva");
        var v2 = await Venda("Joao Souza");
        await Devolucao(v1, "Produto com defeito");
        await Devolucao(v2, "Tamanho errado");

        using var s = _db.NovaSessao();
        var r = await new DevolucoesRepository(s).PesquisarDevolucoes("", vendaId: v2);

        var item = Assert.Single(r.Itens);
        Assert.Equal(v2, item.VendaId);
        Assert.Equal("Joao Souza", item.ClienteNome);
        Assert.Equal(1, r.TotalDeItens);
    }

    [Fact]
    public async Task Pagina_do_mais_recente_para_o_mais_antigo()
    {
        var v = await Venda("Maria Silva");
        await Devolucao(v, "Motivo antigo", 3);
        await Devolucao(v, "Motivo medio", 2);
        await Devolucao(v, "Motivo novo", 1);

        using var s = _db.NovaSessao();
        var repo = new DevolucoesRepository(s);
        var p1 = await repo.PesquisarDevolucoes("", 1, 2);
        var p2 = await repo.PesquisarDevolucoes("", 2, 2);

        Assert.Equal(3, p1.TotalDeItens);
        Assert.Equal(2, p1.TotalDePaginas);
        Assert.Equal(new[] { "Motivo novo", "Motivo medio" }, p1.Itens.Select(i => i.Motivo));
        Assert.Equal("Motivo antigo", Assert.Single(p2.Itens).Motivo);
    }

    [Fact]
    public async Task Pesquisa_por_cliente_motivo_sem_acento()
    {
        var v = await Venda("José Ação");
        await Devolucao(v, "Produto com defeito");

        using var s = _db.NovaSessao();
        var repo = new DevolucoesRepository(s);
        Assert.Single((await repo.PesquisarDevolucoes("jose acao")).Itens);
        Assert.Single((await repo.PesquisarDevolucoes("defeito")).Itens);
        Assert.Empty((await repo.PesquisarDevolucoes("inexistente")).Itens);
    }
}
