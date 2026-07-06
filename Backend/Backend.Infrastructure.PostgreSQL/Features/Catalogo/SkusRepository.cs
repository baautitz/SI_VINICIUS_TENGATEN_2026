using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend.Core.Common.Exceptions;
using Backend.Core.Common.Results;
using Backend.Core.Features.Catalogo.Entities;
using Backend.Core.Features.Catalogo.Repositories;
using Backend.Infrastructure.PostgreSQL.Common;
using Dapper;
using Npgsql;

namespace Backend.Infrastructure.PostgreSQL.Features.Catalogo;

public class SkusRepository : ISkusRepository
{
    private readonly DbSession _session;

    public SkusRepository(DbSession session)
    {
        _session = session;
    }

    public async Task<ResultadoPaginado<Skus>> ObterSkus(int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;
        const string sqlCount = "SELECT COUNT(*) FROM skus;";
        const string sqlData = @"
            SELECT 
                s.sku, s.gtin_ean AS GtinEan, s.preco AS Preco, s.estoque AS Estoque, s.ativo AS Ativo, s.custo_medio AS CustoMedio, s.custo_ultima_compra AS CustoUltimaCompra,
                p.id AS ProdutoId, p.produto AS ProdutoNome, p.descricao AS ProdutoDescricao, p.ativo AS ProdutoAtivo,
                c.id AS CategoriaId, c.categoria AS CategoriaNome, c.descricao AS CategoriaDescricao, c.ativo AS CategoriaAtivo,
                m.id AS MarcaId, m.marca AS MarcaNome, m.descricao AS MarcaDescricao, m.ativo AS MarcaAtivo,
                u.id AS UnidadeMedidaId, u.sigla AS UnidadeMedidaSigla, u.descricao AS UnidadeMedidaDescricao, u.categoria AS UnidadeMedidaCategoria, u.permite_decimais AS PermiteDecimais, u.ativo AS UnidadeMedidaAtivo
            FROM skus s
            JOIN produtos p ON p.id = s.produto_id
            JOIN categorias c ON c.id = p.categoria_id
            JOIN marcas m ON m.id = p.marca_id
            JOIN unidades_medida u ON u.id = p.unidade_medida_id
            ORDER BY s.sku DESC
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        var total = await _session.Connection.ExecuteScalarAsync<int>(sqlCount, transaction: _session.Transaction);
        var rows = (await _session.Connection.QueryAsync<SkuFullDbRow>(
            sqlData,
            new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction)).ToList();

        var skus = rows.Select(BuildSkuFromFullDbRow).ToList();

        if (skus.Count > 0)
        {
            await HydrateAttributes(skus);
        }

        return new ResultadoPaginado<Skus>(skus, total, pagina, tamanhoDaPagina);
    }

    private async Task HydrateAttributes(IEnumerable<Skus> skus)
    {
        var skuCodes = skus.Select(s => s.Sku).ToArray();
        const string atributosSql = @"
            SELECT savr.sku AS Sku, sav.chave_id AS ChaveId, sav.valor AS Valor,
                   sav.id AS Id, sak.chave AS Chave
            FROM skus_atributos_valores_relacionamento savr
            JOIN sku_atributos_valores sav ON sav.id = savr.valor_id
            JOIN sku_atributos_chaves sak ON sak.id = sav.chave_id
            WHERE savr.sku = ANY(@Skus);";

        var atributos = (await _session.Connection.QueryAsync<AtributoDbRow>(
            atributosSql,
            new { Skus = skuCodes },
            transaction: _session.Transaction)).ToList();

        var atributosPorSku = atributos
            .GroupBy(a => a.Sku)
            .ToDictionary(g => g.Key, g => g.AsEnumerable());

        foreach (var sku in skus)
        {
            if (atributosPorSku.TryGetValue(sku.Sku, out var sub))
            {
                foreach (var atributoDbRow in sub)
                {
                    sku.AdicionarAtributo(BuildAtributo(atributoDbRow));
                }
            }
        }
    }

    public async Task<ResultadoPaginado<Skus>> ObterSkusPorProduto(int produtoId)
    {
        const string countSql = "SELECT COUNT(*) FROM skus WHERE produto_id = @ProdutoId;";

        const string skusSql = @"
            SELECT 
                s.sku, s.gtin_ean AS GtinEan, s.preco AS Preco, s.estoque AS Estoque, s.ativo AS Ativo, s.custo_medio AS CustoMedio, s.custo_ultima_compra AS CustoUltimaCompra,
                p.id AS ProdutoId, p.produto AS ProdutoNome, p.descricao AS ProdutoDescricao, p.ativo AS ProdutoAtivo,
                c.id AS CategoriaId, c.categoria AS CategoriaNome, c.descricao AS CategoriaDescricao, c.ativo AS CategoriaAtivo,
                m.id AS MarcaId, m.marca AS MarcaNome, m.descricao AS MarcaDescricao, m.ativo AS MarcaAtivo,
                u.id AS UnidadeMedidaId, u.sigla AS UnidadeMedidaSigla, u.descricao AS UnidadeMedidaDescricao, u.categoria AS UnidadeMedidaCategoria, u.permite_decimais AS PermiteDecimais, u.ativo AS UnidadeMedidaAtivo
            FROM skus s
            JOIN produtos p ON p.id = s.produto_id
            JOIN categorias c ON c.id = p.categoria_id
            JOIN marcas m ON m.id = p.marca_id
            JOIN unidades_medida u ON u.id = p.unidade_medida_id
            WHERE s.produto_id = @ProdutoId
            ORDER BY s.sku;";

        const string atributosSql = @"
            SELECT savr.sku AS Sku, sav.chave_id AS ChaveId, sav.valor AS Valor,
                   sav.id AS Id, sak.chave AS Chave
            FROM skus_atributos_valores_relacionamento savr
            JOIN sku_atributos_valores sav ON sav.id = savr.valor_id
            JOIN sku_atributos_chaves sak ON sak.id = sav.chave_id
            WHERE savr.sku IN (SELECT sku FROM skus WHERE produto_id = @ProdutoId);";

        var total = await _session.Connection.ExecuteScalarAsync<int>(
            countSql, new { ProdutoId = produtoId }, transaction: _session.Transaction);

        var rows = (await _session.Connection.QueryAsync<SkuFullDbRow>(
            skusSql,
            new { ProdutoId = produtoId },
            transaction: _session.Transaction)).ToList();

        var skus = rows.Select(BuildSkuFromFullDbRow).ToList();

        if (skus.Count > 0)
        {
            var atributos = (await _session.Connection.QueryAsync<AtributoDbRow>(
                atributosSql,
                new { ProdutoId = produtoId },
                transaction: _session.Transaction)).ToList();

            var atributosPorSku = atributos
                .GroupBy(a => a.Sku)
                .ToDictionary(g => g.Key, g => g.AsEnumerable());

            foreach (var sku in skus)
            {
                if (atributosPorSku.TryGetValue(sku.Sku, out var sub))
                {
                    foreach (var atributoDbRow in sub)
                    {
                        var atributo = BuildAtributo(atributoDbRow);
                        sku.AdicionarAtributo(atributo);
                    }
                }
            }
        }

        return new ResultadoPaginado<Skus>(skus, total, 1, total > 0 ? total : 1);
    }

    public async Task<Skus?> ObterSkuPorSku(string sku)
    {
        const string skuSql = @"
            SELECT 
                s.sku, s.gtin_ean AS GtinEan, s.preco AS Preco, s.estoque AS Estoque, s.ativo AS Ativo, s.custo_medio AS CustoMedio, s.custo_ultima_compra AS CustoUltimaCompra,
                p.id AS ProdutoId, p.produto AS ProdutoNome, p.descricao AS ProdutoDescricao, p.ativo AS ProdutoAtivo,
                c.id AS CategoriaId, c.categoria AS CategoriaNome, c.descricao AS CategoriaDescricao, c.ativo AS CategoriaAtivo,
                m.id AS MarcaId, m.marca AS MarcaNome, m.descricao AS MarcaDescricao, m.ativo AS MarcaAtivo,
                u.id AS UnidadeMedidaId, u.sigla AS UnidadeMedidaSigla, u.descricao AS UnidadeMedidaDescricao, u.categoria AS UnidadeMedidaCategoria, u.permite_decimais AS PermiteDecimais, u.ativo AS UnidadeMedidaAtivo
            FROM skus s
            JOIN produtos p ON p.id = s.produto_id
            JOIN categorias c ON c.id = p.categoria_id
            JOIN marcas m ON m.id = p.marca_id
            JOIN unidades_medida u ON u.id = p.unidade_medida_id
            WHERE s.sku = @Sku OR s.gtin_ean = @Sku;";

        const string atributosSql = @"
            SELECT savr.sku AS Sku, sav.chave_id AS ChaveId, sav.valor AS Valor,
                   sav.id AS Id, sak.chave AS Chave
            FROM skus_atributos_valores_relacionamento savr
            JOIN sku_atributos_valores sav ON sav.id = savr.valor_id
            JOIN sku_atributos_chaves sak ON sak.id = sav.chave_id
            WHERE savr.sku = @Sku;";

        var row = await _session.Connection.QueryFirstOrDefaultAsync<SkuFullDbRow>(
            skuSql,
            new { Sku = sku },
            transaction: _session.Transaction);

        if (row is null) return null;

        var skuEntity = BuildSkuFromFullDbRow(row);

        var atributos = await _session.Connection.QueryAsync<AtributoDbRow>(
            atributosSql,
            new { Sku = skuEntity.Sku },
            transaction: _session.Transaction);

        foreach (var atributoDbRow in atributos)
        {
            var atributo = BuildAtributo(atributoDbRow);
            skuEntity.AdicionarAtributo(atributo);
        }

        return skuEntity;
    }

    public async Task<Skus> CriarSku(int produtoId, Skus skuData)
    {
        const string sql = @"
            INSERT INTO skus (sku, gtin_ean, preco, estoque, ativo, produto_id, custo_medio, custo_ultima_compra)
            VALUES (@Sku, @GtinEan, @Preco, @Estoque, @Ativo, @ProdutoId, @CustoMedio, @CustoUltimaCompra);";

        await _session.Connection.ExecuteAsync(
            sql,
            new
            {
                skuData.Sku,
                skuData.GtinEan,
                skuData.Preco,
                skuData.Estoque,
                skuData.Ativo,
                skuData.CustoMedio,
                skuData.CustoUltimaCompra,
                ProdutoId = produtoId
            },
            transaction: _session.Transaction);

        await InserirAtributos(skuData.Sku, skuData.Atributos);

        return skuData;
    }

    public async Task<Skus> AtualizarSku(string sku, Skus skuData)
    {
        const string sql = @"
            UPDATE skus
            SET gtin_ean = @GtinEan, preco = @Preco, estoque = @Estoque, ativo = @Ativo, custo_medio = @CustoMedio, custo_ultima_compra = @CustoUltimaCompra
            WHERE sku = @Sku;";

        await _session.Connection.ExecuteAsync(
            sql,
            new { Sku = sku, skuData.GtinEan, skuData.Preco, skuData.Estoque, skuData.Ativo, skuData.CustoMedio, skuData.CustoUltimaCompra },
            transaction: _session.Transaction);

        await ReplacerAtributos(sku, skuData.Atributos);

        return skuData;
    }

    public async Task<bool> DeletarSku(string sku)
    {
        try
        {
            await _session.Connection.ExecuteAsync(
                "DELETE FROM skus_atributos_valores_relacionamento WHERE sku = @Sku;",
                new { Sku = sku }, transaction: _session.Transaction);

            var linhasAfetadas = await _session.Connection.ExecuteAsync(
                "DELETE FROM skus WHERE sku = @Sku;",
                new { Sku = sku }, transaction: _session.Transaction);

            return linhasAfetadas > 0;
        }
        catch (PostgresException ex)
        {
            throw DbExceptionTranslator.Translate(ex);
        }
    }

    public async Task<ResultadoPaginado<Skus>> PesquisarSkus(string termo, int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sqlCount = @"
            SELECT COUNT(*)
            FROM skus s
            JOIN produtos p ON p.id = s.produto_id
            WHERE s.sku ILIKE @Termo OR s.gtin_ean ILIKE @Termo OR p.produto ILIKE @Termo;";

        const string sqlData = @"
            SELECT 
                s.sku, s.gtin_ean AS GtinEan, s.preco AS Preco, s.estoque AS Estoque, s.ativo AS Ativo, s.custo_medio AS CustoMedio, s.custo_ultima_compra AS CustoUltimaCompra,
                p.id AS ProdutoId, p.produto AS ProdutoNome, p.descricao AS ProdutoDescricao, p.ativo AS ProdutoAtivo,
                c.id AS CategoriaId, c.categoria AS CategoriaNome, c.descricao AS CategoriaDescricao, c.ativo AS CategoriaAtivo,
                m.id AS MarcaId, m.marca AS MarcaNome, m.descricao AS MarcaDescricao, m.ativo AS MarcaAtivo,
                u.id AS UnidadeMedidaId, u.sigla AS UnidadeMedidaSigla, u.descricao AS UnidadeMedidaDescricao, u.categoria AS UnidadeMedidaCategoria, u.permite_decimais AS PermiteDecimais, u.ativo AS UnidadeMedidaAtivo
            FROM skus s
            JOIN produtos p ON p.id = s.produto_id
            JOIN categorias c ON c.id = p.categoria_id
            JOIN marcas m ON m.id = p.marca_id
            JOIN unidades_medida u ON u.id = p.unidade_medida_id
            WHERE s.sku ILIKE @Termo OR s.gtin_ean ILIKE @Termo OR p.produto ILIKE @Termo
            ORDER BY s.sku DESC
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        var total = await _session.Connection.ExecuteScalarAsync<int>(sqlCount, new { Termo = $"%{termo}%" }, transaction: _session.Transaction);

        var rows = (await _session.Connection.QueryAsync<SkuFullDbRow>(
            sqlData,
            new { Termo = $"%{termo}%", TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction)).ToList();

        var skus = rows.Select(BuildSkuFromFullDbRow).ToList();

        if (skus.Count > 0)
        {
            await HydrateAttributes(skus);
        }

        return new ResultadoPaginado<Skus>(skus, total, pagina, tamanhoDaPagina);
    }

    public Task<Skus?> ObterSkuCompleto(string sku) => ObterSkuPorSku(sku);

    public async Task<Produtos?> ObterProdutoPorSku(string sku)
    {
        const string sql = @"
            SELECT p.id, p.produto, p.descricao, p.ativo
            FROM produtos p
            JOIN skus s ON s.produto_id = p.id
            WHERE s.sku = @Sku OR s.gtin_ean = @Sku;";

        return await _session.Connection.QueryFirstOrDefaultAsync<Produtos>(
            sql, new { Sku = sku }, transaction: _session.Transaction);
    }

    private async Task InserirAtributos(string skuCodigo, IEnumerable<SkuAtributosValores> atributos)
    {
        const string sql = @"
            INSERT INTO skus_atributos_valores_relacionamento (sku, valor_id)
            VALUES (@Sku, @ValorId);";

        await _session.Connection.ExecuteAsync(
            sql,
            atributos.Select(a => new
            {
                Sku = skuCodigo,
                ValorId = a.Id
            }),
            transaction: _session.Transaction);
    }

    private async Task ReplacerAtributos(string skuCodigo, IEnumerable<SkuAtributosValores> atributos)
    {
        await _session.Connection.ExecuteAsync(
            "DELETE FROM skus_atributos_valores_relacionamento WHERE sku = @Sku;",
            new { Sku = skuCodigo }, transaction: _session.Transaction);

        await InserirAtributos(skuCodigo, atributos);
    }

    private static Skus BuildSkuFromFullDbRow(SkuFullDbRow row)
    {
        var categoria = new Categorias(row.CategoriaId, row.CategoriaNome, row.CategoriaDescricao);
        if (!row.CategoriaAtivo) categoria.Desativar();

        var marca = new Marcas(row.MarcaId, row.MarcaNome, row.MarcaDescricao);
        if (!row.MarcaAtivo) marca.Desativar();

        var unidadeMedida = new UnidadesMedida(
            row.UnidadeMedidaSigla,
            row.UnidadeMedidaDescricao,
            row.UnidadeMedidaCategoria,
            row.PermiteDecimais,
            row.UnidadeMedidaAtivo)
        {
            Id = row.UnidadeMedidaId
        };

        var produto = new Produtos(row.ProdutoId, row.ProdutoNome, row.ProdutoDescricao, categoria, marca, unidadeMedida);
        if (!row.ProdutoAtivo) produto.Desativar();

        var sku = new Skus(row.Sku, row.Preco, row.Estoque, row.Ativo, row.GtinEan, row.CustoMedio, row.CustoUltimaCompra, produto);
        return sku;
    }

    private static SkuAtributosValores BuildAtributo(AtributoDbRow row)
    {
        return new SkuAtributosValores(row.Id, row.ChaveId, row.Valor);
    }

    private sealed class SkuFullDbRow
    {
        public string Sku { get; set; } = null!;
        public string? GtinEan { get; set; }
        public decimal Preco { get; set; }
        public decimal Estoque { get; set; }
        public bool Ativo { get; set; }
        public decimal CustoMedio { get; set; }
        public decimal CustoUltimaCompra { get; set; }

        public int ProdutoId { get; set; }
        public string ProdutoNome { get; set; } = null!;
        public string? ProdutoDescricao { get; set; }
        public bool ProdutoAtivo { get; set; }

        public int CategoriaId { get; set; }
        public string CategoriaNome { get; set; } = null!;
        public string? CategoriaDescricao { get; set; }
        public bool CategoriaAtivo { get; set; }

        public int MarcaId { get; set; }
        public string MarcaNome { get; set; } = null!;
        public string? MarcaDescricao { get; set; }
        public bool MarcaAtivo { get; set; }

        public int UnidadeMedidaId { get; set; }
        public string UnidadeMedidaSigla { get; set; } = null!;
        public string UnidadeMedidaDescricao { get; set; } = null!;
        public string UnidadeMedidaCategoria { get; set; } = null!;
        public bool UnidadeMedidaAtivo { get; set; }
        public bool PermiteDecimais { get; set; }
    }

    private sealed record SkuDbRow(string Sku, string? GtinEan, decimal Preco, decimal Estoque, bool Ativo, decimal CustoMedio, decimal CustoUltimaCompra);
    private sealed record AtributoDbRow(string Sku, int ChaveId, string Valor, int Id, string Chave);
}
