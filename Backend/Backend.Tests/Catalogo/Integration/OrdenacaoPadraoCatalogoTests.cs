using Backend.Infrastructure.PostgreSQL.Features.Catalogo;
using Backend.Tests.Common;
using Dapper;

namespace Backend.Tests.Catalogo.Integration;

// ADR 0002: o catálogo abre em ordem alfabética por nome (SKUs pelo código), com desempate pelo id.
[Trait("Category", "Integration")]
[Collection(PostgresCollection.Nome)]
public class OrdenacaoPadraoCatalogoTests
{
    private readonly PostgresFixture _db;

    public OrdenacaoPadraoCatalogoTests(PostgresFixture db)
    {
        _db = db;
        _db.Executar("TRUNCATE skus, produtos, marcas, categorias, unidades_medida, sku_atributos_chaves RESTART IDENTITY CASCADE;").GetAwaiter().GetResult();
    }

    private async Task<int> Inserir(string sql, object? p = null)
    {
        using var s = _db.NovaSessao();
        return await s.Connection.ExecuteScalarAsync<int>(sql, p);
    }

    private async Task<(int c, int m, int u)> Base()
    {
        var c = await Inserir("INSERT INTO categorias (categoria) VALUES ('C') RETURNING id;");
        var m = await Inserir("INSERT INTO marcas (marca) VALUES ('M') RETURNING id;");
        var u = await Inserir("INSERT INTO unidades_medida (sigla, descricao, categoria) VALUES ('UN', 'Unidade', 'Contagem') RETURNING id;");
        return (c, m, u);
    }

    [Fact]
    public async Task Marcas_AbremEmOrdemAlfabetica_IgnorandoCaixaEAcento()
    {
        foreach (var n in new[] { "Zeta", "alfa", "Émile", "Beta" })
            await Inserir("INSERT INTO marcas (marca) VALUES (@n) RETURNING id;", new { n });
        using var s = _db.NovaSessao();
        var r = await new MarcasRepository(s).ObterMarcas();
        Assert.Equal(new[] { "alfa", "Beta", "Émile", "Zeta" }, r.Itens.Select(x => x.Marca));
    }

    [Fact]
    public async Task Marcas_PaginacaoConsistente()
    {
        foreach (var n in new[] { "D", "B", "A", "C", "E" })
            await Inserir("INSERT INTO marcas (marca) VALUES (@n) RETURNING id;", new { n });
        using var s = _db.NovaSessao();
        var repo = new MarcasRepository(s);
        var todas = new List<string>();
        for (var p = 1; p <= 3; p++) todas.AddRange((await repo.ObterMarcas(p, 2)).Itens.Select(x => x.Marca));
        Assert.Equal(new[] { "A", "B", "C", "D", "E" }, todas);
    }

    [Fact]
    public async Task Marcas_PesquisaTambemAbreEmOrdemAlfabetica()
    {
        foreach (var n in new[] { "Marca Z", "Marca A" })
            await Inserir("INSERT INTO marcas (marca) VALUES (@n) RETURNING id;", new { n });
        using var s = _db.NovaSessao();
        var r = await new MarcasRepository(s).PesquisarMarcas("%Marca%");
        Assert.Equal(new[] { "Marca A", "Marca Z" }, r.Itens.Select(x => x.Marca));
    }

    [Fact]
    public async Task Categorias_AbremEmOrdemAlfabetica()
    {
        foreach (var n in new[] { "Z", "A", "M" })
            await Inserir("INSERT INTO categorias (categoria) VALUES (@n) RETURNING id;", new { n });
        using var s = _db.NovaSessao();
        var r = await new CategoriasRepository(s).ObterCategorias();
        Assert.Equal(new[] { "A", "M", "Z" }, r.Itens.Select(x => x.Categoria));
    }

    [Fact]
    public async Task UnidadesMedida_AbremEmOrdemAlfabeticaPorDescricao()
    {
        await Inserir("INSERT INTO unidades_medida (sigla, descricao, categoria) VALUES ('KG','Quilograma','Massa') RETURNING id;");
        await Inserir("INSERT INTO unidades_medida (sigla, descricao, categoria) VALUES ('CX','Caixa','Contagem') RETURNING id;");
        using var s = _db.NovaSessao();
        var r = await new UnidadesMedidaRepository(s).ObterUnidadesMedida();
        Assert.Equal(new[] { "CX", "KG" }, r.Itens.Select(x => x.Sigla));
    }

    [Fact]
    public async Task Atributos_AbremEmOrdemAlfabetica()
    {
        foreach (var n in new[] { "Tamanho", "Cor" })
            await Inserir("INSERT INTO sku_atributos_chaves (chave) VALUES (@n) RETURNING id;", new { n });
        using var s = _db.NovaSessao();
        var r = await new SkuAtributosChavesRepository(s).ObterAtributos();
        Assert.Equal(new[] { "COR", "TAMANHO" }, r.Itens.Select(x => x.Chave));
    }

    [Fact]
    public async Task Produtos_AbremEmOrdemAlfabetica_ComDesempatePeloId()
    {
        var (c, m, u) = await Base();
        foreach (var n in new[] { "Zebra", "Abajur", "Abajur" })
            await Inserir("INSERT INTO produtos (produto, descricao, categoria_id, marca_id, unidade_medida_id) VALUES (@n,'d',@c,@m,@u) RETURNING id;", new { n, c, m, u });
        using var s = _db.NovaSessao();
        var r = await new ProdutosRepository(s).ObterProdutos();
        Assert.Equal(new[] { 2, 3, 1 }, r.Itens.Select(x => x.Id));
    }

    [Fact]
    public async Task Skus_AbremPeloCodigoCrescente_EBuscaExataContinuaPrimeiro()
    {
        var (c, m, u) = await Base();
        var p = await Inserir("INSERT INTO produtos (produto, descricao, categoria_id, marca_id, unidade_medida_id) VALUES ('P','d',@c,@m,@u) RETURNING id;", new { c, m, u });
        foreach (var sku in new[] { "SKU-C", "SKU-A", "SKU-B" })
            await Inserir("INSERT INTO skus (sku, produto_id, preco) VALUES (@sku,@p,1) RETURNING produto_id;", new { sku, p });
        using var s = _db.NovaSessao();
        var repo = new SkusRepository(s);
        Assert.Equal(new[] { "SKU-A", "SKU-B", "SKU-C" }, (await repo.ObterSkus()).Itens.Select(x => x.Sku));
        Assert.Equal("SKU-C", (await repo.PesquisarSkus("SKU-C")).Itens.First().Sku);
    }
}
