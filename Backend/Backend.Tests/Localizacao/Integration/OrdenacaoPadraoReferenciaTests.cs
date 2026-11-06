using Backend.Infrastructure.PostgreSQL.Features.Acesso;
using Backend.Infrastructure.PostgreSQL.Features.Financeiro;
using Backend.Infrastructure.PostgreSQL.Features.Localizacao;
using Backend.Tests.Common;
using Dapper;

namespace Backend.Tests.Localizacao.Integration;

// ADR 0002: tabelas de referencia abrem em ordem alfabetica, com desempate estavel pelo id.
[Trait("Category", "Integration")]
[Collection(PostgresCollection.Nome)]
public class OrdenacaoPadraoReferenciaTests
{
    private readonly PostgresFixture _db;

    public OrdenacaoPadraoReferenciaTests(PostgresFixture db) => _db = db;

    private async Task<int> Inserir(string sql, object param)
    {
        using var s = _db.NovaSessao();
        return await s.Connection.ExecuteScalarAsync<int>(sql, param);
    }

    private static string Sufixo() => Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();

    [Fact]
    public async Task Paises_abrem_em_ordem_alfabetica_ignorando_caixa_e_acento()
    {
        var p = Sufixo();
        string[] nomes = [$"{p}Abc", $"{p}ágata", $"{p}Ilha", $"Zz{p}"];
        for (var i = 0; i < nomes.Length; i++)
            await Inserir("INSERT INTO paises (ddi, codigo_iso_pais, codigo_iso_moeda, simbolo_moeda, pais) VALUES ('+1', @iso, 'XXX', '$', @nome) RETURNING id;", new { iso = $"{p[..2]}{i}", nome = nomes[i] });

        using var s = _db.NovaSessao();
        var repo = new PaisesRepository(s);
        string[] esperado = [$"{p}Abc", $"{p}ágata", $"{p}Ilha", $"Zz{p}"];

        var lista = await repo.ObterPaises(1, 1000);
        Assert.Equal(esperado, lista.Itens.Select(x => x.Pais).Where(n => n.Contains(p)));
        var pesquisa = await repo.PesquisarPaises(p, 1, 50);
        Assert.Equal(esperado, pesquisa.Itens.Select(x => x.Pais));
    }

    [Fact]
    public async Task Estados_cidades_e_bairros_abrem_em_ordem_alfabetica()
    {
        var p = Sufixo();
        var pais = await Inserir("INSERT INTO paises (ddi, codigo_iso_pais, codigo_iso_moeda, simbolo_moeda, pais) VALUES ('+1', @iso, 'XXX', '$', @nome) RETURNING id;", new { iso = p[..3], nome = $"Pais {p}" });
        var ids = new List<int>();
        foreach (var (nome, uf) in new[] { ("Zeta", "ZZ"), ("Alfa", "AA"), ("Meio", "MM") })
            ids.Add(await Inserir("INSERT INTO estados (pais_id, estado, uf) VALUES (@pais, @nome, @uf) RETURNING id;", new { pais, nome, uf }));
        var estado = ids[0];
        var cidadeIds = new List<int>();
        foreach (var nome in new[] { "Zeta", "Alfa", "Meio" })
            cidadeIds.Add(await Inserir("INSERT INTO cidades (cidade, ddd, estado_id) VALUES (@nome, '11', @estado) RETURNING id;", new { nome, estado }));
        var cidade = cidadeIds[0];
        foreach (var nome in new[] { "Zeta", "Alfa", "Meio" })
            await Inserir("INSERT INTO bairros (bairro, cidade_id) VALUES (@nome, @cidade) RETURNING id;", new { nome, cidade });

        using var s = _db.NovaSessao();
        string[] esperado = ["ALFA", "MEIO", "ZETA"];
        Assert.Equal(esperado, (await new EstadosRepository(s).ObterEstados(1, 20, pais)).Itens.Select(x => x.Estado.ToUpperInvariant()));
        Assert.Equal(esperado, (await new EstadosRepository(s).PesquisarEstados("", 1, 20, pais)).Itens.Select(x => x.Estado.ToUpperInvariant()));
        Assert.Equal(esperado, (await new CidadesRepository(s).ObterCidades(1, 20, estado)).Itens.Select(x => x.Cidade.ToUpperInvariant()));
        Assert.Equal(esperado, (await new CidadesRepository(s).PesquisarCidades("", 1, 20, estado)).Itens.Select(x => x.Cidade.ToUpperInvariant()));
        Assert.Equal(esperado, (await new BairrosRepository(s).ObterBairros(1, 20, cidade)).Itens.Select(x => x.Bairro.ToUpperInvariant()));
        Assert.Equal(esperado, (await new BairrosRepository(s).PesquisarBairros("", 1, 20, cidade)).Itens.Select(x => x.Bairro.ToUpperInvariant()));
    }

    [Fact]
    public async Task Condicoes_de_pagamento_abrem_em_ordem_alfabetica()
    {
        var p = Sufixo();
        var codigo = $"M{p[..4]}";
        await _db.Executar("INSERT INTO metodos_pagamento (codigo, descricao) VALUES (@codigo, 'Metodo');", new { codigo });
        foreach (var d in new[] { $"{p} Zeta", $"{p} alfa", $"{p} Meio" })
            await Inserir("INSERT INTO condicoes_pagamentos (descricao, metodo_pagamento_codigo) VALUES (@d, @codigo) RETURNING id;", new { d, codigo });

        using var s = _db.NovaSessao();
        string[] esperado = [$"{p} alfa", $"{p} Meio", $"{p} Zeta"];
        var lista = await new CondicoesPagamentosRepository(s).ObterCondicoesPagamentos(1, 1000);
        Assert.Equal(esperado, lista.Itens.Select(x => x.Descricao).Where(d => d.Contains(p)));
        var pesquisa = await new CondicoesPagamentosRepository(s).PesquisarCondicoesPagamentos(p);
        Assert.Equal(esperado, pesquisa.Itens.Select(x => x.Descricao));
    }

    [Fact]
    public async Task Usuarios_abrem_em_ordem_alfabetica_com_desempate_estavel_pelo_id_entre_paginas()
    {
        var p = Sufixo();
        var ordem = new List<int>();
        var i = 0;
        foreach (var nome in new[] { $"{p} Zeca", $"{p} ana", $"{p} Zeca", $"{p} Bia" })
        {
            i++;
            ordem.Add(await Inserir("INSERT INTO usuarios (nome, cpf_cnpj, email, usuario, senha) VALUES (@nome, @cpf, 'a@b.com', @usuario, 'x') RETURNING id;", new { nome, cpf = $"{p}{i:D5}", usuario = $"u{p}{i}" }));
        }

        using var s = _db.NovaSessao();
        var repo = new UsuariosRepository(s);
        int[] esperado = [ordem[1], ordem[3], ordem[0], ordem[2]];

        var resumo = await repo.PesquisarUsuarios(p, 1, 50);
        Assert.Equal(esperado, resumo.Itens.Select(x => x.Id));

        var paginado = new List<int>();
        for (var pagina = 1; pagina <= 2; pagina++)
            paginado.AddRange((await repo.PesquisarUsuarios(p, pagina, 2)).Itens.Select(x => x.Id));
        Assert.Equal(esperado, paginado);

        var completo = (await repo.ObterUsuarios(1, 1000)).Itens.Where(u => u.Nome.StartsWith(p)).Select(u => u.Id);
        Assert.Equal(esperado, completo);
        var resumido = (await repo.ObterUsuariosResumo(1, 1000)).Itens.Where(u => u.Nome.StartsWith(p)).Select(u => u.Id);
        Assert.Equal(esperado, resumido);
    }
}
