using Backend.Infrastructure.PostgreSQL.Features.Parceiros;

namespace Backend.Tests.Parceiros.Integration;

using Backend.Tests.Common;

// ADR 0002: parceiros abrem em ordem alfabetica (sem caixa nem acento), desempate por id.
[Trait("Category", "Integration")]
[Collection(PostgresCollection.Nome)]
public class OrdenacaoPadraoParceirosTests
{
    private readonly PostgresFixture _db;

    public OrdenacaoPadraoParceirosTests(PostgresFixture db)
    {
        _db = db;
        _db.Executar("TRUNCATE veiculos, emitentes, contas_pagar, contas_receber, fornecedores, clientes, transportadoras RESTART IDENTITY CASCADE;").GetAwaiter().GetResult();
    }

    private async Task Inserir(string tabela, params string[] nomes)
    {
        var i = 0;
        foreach (var nome in nomes)
            await _db.Executar($"INSERT INTO {tabela} (tipo_pessoa, nome_razaosocial, cpf_cnpj, nacionalidade_id) VALUES ('JURIDICA', @nome, @doc, (SELECT id FROM paises LIMIT 1));",
                new { nome, doc = $"{++i:D14}" });
    }

    private static readonly string[] Nomes = ["Zeca", "Ana", "ábaco", "Bruno", "Ana"];
    private static readonly string[] Esperado = ["ÁBACO", "ANA", "ANA", "BRUNO", "ZECA"];

    [Fact]
    public async Task Clientes_abrem_em_ordem_alfabetica_com_desempate_por_id()
    {
        await Inserir("clientes", Nomes);
        using var s = _db.NovaSessao();
        var repo = new ClientesRepository(s);

        var lista = await repo.ObterClientes();
        Assert.Equal(Esperado, lista.Itens.Select(c => c.NomeRazaoSocial));
        Assert.True(lista.Itens.ElementAt(1).Id < lista.Itens.ElementAt(2).Id);

        var pesquisa = await repo.PesquisarClientes("a");
        Assert.Equal(["ÁBACO", "ANA", "ANA", "ZECA"], pesquisa.Itens.Select(c => c.NomeRazaoSocial));

        var pag2 = await repo.ObterClientes(2, 2);
        Assert.Equal(["ANA", "BRUNO"], pag2.Itens.Select(c => c.NomeRazaoSocial));
    }

    [Fact]
    public async Task Fornecedores_abrem_em_ordem_alfabetica_com_desempate_por_id()
    {
        await Inserir("fornecedores", Nomes);
        using var s = _db.NovaSessao();
        var repo = new FornecedoresRepository(s);

        var lista = await repo.ObterFornecedores();
        Assert.Equal(Esperado, lista.Itens.Select(f => f.NomeRazaosocial));
        Assert.True(lista.Itens.ElementAt(1).Id < lista.Itens.ElementAt(2).Id);

        var pesquisa = await repo.PesquisarFornecedores("a");
        Assert.Equal(["ÁBACO", "ANA", "ANA", "ZECA"], pesquisa.Itens.Select(f => f.NomeRazaosocial));

        var pag2 = await repo.ObterFornecedores(2, 2);
        Assert.Equal(["ANA", "BRUNO"], pag2.Itens.Select(f => f.NomeRazaosocial));
    }

    [Fact]
    public async Task Transportadoras_abrem_em_ordem_alfabetica_com_desempate_por_id()
    {
        await Inserir("transportadoras", Nomes);
        using var s = _db.NovaSessao();
        var repo = new TransportadorasRepository(s);

        var lista = await repo.ObterTransportadoras();
        Assert.Equal(Esperado, lista.Itens.Select(t => t.NomeRazaosocial));
        Assert.True(lista.Itens.ElementAt(1).Id < lista.Itens.ElementAt(2).Id);

        var pesquisa = await repo.PesquisarTransportadoras("a");
        Assert.Equal(["ÁBACO", "ANA", "ANA", "ZECA"], pesquisa.Itens.Select(t => t.NomeRazaosocial));

        var pag2 = await repo.ObterTransportadoras(2, 2);
        Assert.Equal(["ANA", "BRUNO"], pag2.Itens.Select(t => t.NomeRazaosocial));
    }

    [Fact]
    public async Task Emitentes_abrem_em_ordem_alfabetica_com_desempate_por_id()
    {
        await Inserir("emitentes", Nomes);
        using var s = _db.NovaSessao();
        var repo = new EmitentesRepository(s);

        var lista = await repo.ObterEmitentes();
        Assert.Equal(Esperado, lista.Itens.Select(e => e.NomeRazaoSocial));
        Assert.True(lista.Itens.ElementAt(1).Id < lista.Itens.ElementAt(2).Id);

        var pesquisa = await repo.PesquisarEmitentes("a");
        Assert.Equal(["ÁBACO", "ANA", "ANA", "ZECA"], pesquisa.Itens.Select(e => e.NomeRazaoSocial));

        var pag2 = await repo.ObterEmitentes(2, 2);
        Assert.Equal(["ANA", "BRUNO"], pag2.Itens.Select(e => e.NomeRazaoSocial));
    }

    [Fact]
    public async Task Veiculos_abrem_em_ordem_alfabetica_de_placa_com_desempate_por_id()
    {
        foreach (var placa in new[] { "ZZZ9999", "abc1234", "ABC1234", "MMM0000" })
            await _db.Executar("INSERT INTO veiculos (estado_id, placa) VALUES ((SELECT id FROM estados LIMIT 1), @placa);", new { placa });
        using var s = _db.NovaSessao();
        var repo = new VeiculosRepository(s);

        var lista = await repo.ObterVeiculos();
        Assert.Equal(["abc1234", "ABC1234", "MMM0000", "ZZZ9999"], lista.Itens.Select(v => v.Placa));
        Assert.True(lista.Itens.ElementAt(0).Id < lista.Itens.ElementAt(1).Id);

        var pesquisa = await repo.PesquisarVeiculos("abc");
        Assert.Equal(["abc1234", "ABC1234"], pesquisa.Itens.Select(v => v.Placa));

        var pag2 = await repo.ObterVeiculos(2, 2);
        Assert.Equal(["MMM0000", "ZZZ9999"], pag2.Itens.Select(v => v.Placa));
    }
}
