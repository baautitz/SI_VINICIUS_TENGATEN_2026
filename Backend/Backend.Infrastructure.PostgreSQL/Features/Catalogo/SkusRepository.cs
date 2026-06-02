using System.Linq;
using Backend.Core.Common.Results;
using Backend.Core.Features.Catalogo.DTOs;
using Backend.Core.Features.Catalogo.Entities;
using Backend.Core.Features.Catalogo.Repositories;
using Backend.Infrastructure.PostgreSQL.Common;
using Dapper;

namespace Backend.Infrastructure.PostgreSQL.Features.Catalogo;

public class SkusRepository : ISkusRepository
{
    private readonly DbSession _session;

    public SkusRepository(DbSession session)
    {
        _session = session;
    }

    public async Task<ResultadoPaginado<Skus>> ObterSkusPorProduto(int produtoId)
    {
        const string countSql = "SELECT COUNT(*) FROM skus WHERE produto_id = @ProdutoId;";

        const string skusSql = @"
            SELECT sku, gtin_ean AS GtinEan, preco AS Preco, estoque AS Estoque, ativo AS Ativo
            FROM skus
            WHERE produto_id = @ProdutoId
            ORDER BY sku;";

        const string atributosSql = @"
            SELECT sav.sku AS Sku, sav.chave_id AS ChaveId, sav.valor AS Valor,
                   sak.id AS Id, sak.chave AS Chave
            FROM skus_atributos_valores sav
            JOIN sku_atributos_chaves sak ON sak.id = sav.chave_id
            WHERE sav.sku IN (SELECT sku FROM skus WHERE produto_id = @ProdutoId);";

        var total = await _session.Connection.ExecuteScalarAsync<int>(
            countSql, new { ProdutoId = produtoId }, transaction: _session.Transaction);

        var skusDto = (await _session.Connection.QueryAsync<SkuDto>(
            skusSql, new { ProdutoId = produtoId }, transaction: _session.Transaction)).ToList();

        var skus = skusDto.Select(BuildSku).ToList();

        if (skus.Count > 0)
        {
            var atributos = (await _session.Connection.QueryAsync<AtributoDto>(
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
                    foreach (var atributoDto in sub)
                    {
                        var atributo = BuildAtributo(atributoDto, sku);
                        sku.AdicionarAtributo(atributo);
                    }
                }
            }
        }

        return new ResultadoPaginado<Skus>(skus, total, 1, total > 0 ? total : 1);
    }

    public async Task<Skus?> ObterSkuPorSku(string sku)
    {
        const string skuSql = "SELECT sku, gtin_ean AS GtinEan, preco AS Preco, estoque AS Estoque, ativo AS Ativo FROM skus WHERE sku = @Sku;";

        const string atributosSql = @"
            SELECT sav.sku AS Sku, sav.chave_id AS ChaveId, sav.valor AS Valor,
                   sak.id AS Id, sak.chave AS Chave
            FROM skus_atributos_valores sav
            JOIN sku_atributos_chaves sak ON sak.id = sav.chave_id
            WHERE sav.sku = @Sku;";

        var skuDto = await _session.Connection.QuerySingleOrDefaultAsync<SkuDto>(
            skuSql, new { Sku = sku }, transaction: _session.Transaction);

        if (skuDto is null) return null;

        var skuEntity = BuildSku(skuDto);

        var atributos = await _session.Connection.QueryAsync<AtributoDto>(
            atributosSql,
            new { Sku = sku },
            transaction: _session.Transaction);

        foreach (var atributoDto in atributos)
        {
            var atributo = BuildAtributo(atributoDto, skuEntity);
            skuEntity.AdicionarAtributo(atributo);
        }

        return skuEntity;
    }

    public async Task<Skus> CriarSku(int produtoId, Skus skuData)
    {
        const string sql = @"
            INSERT INTO skus (sku, gtin_ean, preco, estoque, ativo, produto_id)
            VALUES (@Sku, @GtinEan, @Preco, @Estoque, @Ativo, @ProdutoId);";

        await _session.Connection.ExecuteAsync(
            sql,
            new
            {
                skuData.Sku,
                skuData.GtinEan,
                skuData.Preco,
                skuData.Estoque,
                skuData.Ativo,
                ProdutoId = produtoId
            },
            transaction: _session.Transaction);

        await InserirAtributos(skuData.Sku, skuData.SkusAtributosValores);

        return skuData;
    }

    public async Task<Skus> AtualizarSku(string sku, Skus skuData)
    {
        const string sql = @"
            UPDATE skus
            SET gtin_ean = @GtinEan, preco = @Preco, estoque = @Estoque, ativo = @Ativo
            WHERE sku = @Sku;";

        await _session.Connection.ExecuteAsync(
            sql,
            new { Sku = sku, skuData.GtinEan, skuData.Preco, skuData.Estoque, skuData.Ativo },
            transaction: _session.Transaction);

        await ReplacerAtributos(sku, skuData.SkusAtributosValores);

        return skuData;
    }

    public async Task<bool> DeletarSku(string sku)
    {
        await _session.Connection.ExecuteAsync(
            "DELETE FROM skus_atributos_valores WHERE sku = @Sku;",
            new { Sku = sku }, transaction: _session.Transaction);

        var linhasAfetadas = await _session.Connection.ExecuteAsync(
            "DELETE FROM skus WHERE sku = @Sku;",
            new { Sku = sku }, transaction: _session.Transaction);

        return linhasAfetadas > 0;
    }

    public async Task<ResultadoPaginado<SkusResumo>> PesquisarSkus(string termo, int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sql = @"
            SELECT COUNT(*)
            FROM skus
            WHERE sku ILIKE @Termo OR gtin_ean ILIKE @Termo;

            SELECT sku, gtin_ean AS GtinEan, preco AS Preco, estoque AS Estoque, ativo AS Ativo
            FROM skus
            WHERE sku ILIKE @Termo OR gtin_ean ILIKE @Termo
            ORDER BY sku
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql,
            new { Termo = $"%{termo}%", TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction);

        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<SkusResumo>();

        return new ResultadoPaginado<SkusResumo>(itens, total, pagina, tamanhoDaPagina);
    }

    private async Task InserirAtributos(string skuCodigo, IEnumerable<SkusAtributosValores> atributos)
    {
        const string sql = @"
            INSERT INTO skus_atributos_valores (sku, chave_id, valor)
            VALUES (@Sku, @ChaveId, @Valor);";

        await _session.Connection.ExecuteAsync(
            sql,
            atributos.Select(a => new
            {
                Sku = skuCodigo,
                ChaveId = a.SkuAtributoChave!.Id,
                a.Valor
            }),
            transaction: _session.Transaction);
    }

    private async Task ReplacerAtributos(string skuCodigo, IEnumerable<SkusAtributosValores> atributos)
    {
        await _session.Connection.ExecuteAsync(
            "DELETE FROM skus_atributos_valores WHERE sku = @Sku;",
            new { Sku = skuCodigo }, transaction: _session.Transaction);

        await InserirAtributos(skuCodigo, atributos);
    }

    private static Skus BuildSku(SkuDto dto)
    {
        return new Skus(dto.Sku, dto.Preco, dto.Estoque, dto.Ativo, dto.GtinEan);
    }

    private static SkusAtributosValores BuildAtributo(AtributoDto dto, Skus sku)
    {
        var chave = new SkuAtributosChaves(dto.Id, dto.Chave);
        return new SkusAtributosValores(dto.Sku, dto.ChaveId, dto.Valor, sku, chave);
    }

    private sealed record SkuDto(string Sku, string? GtinEan, decimal Preco, decimal Estoque, bool Ativo);
    private sealed record AtributoDto(string Sku, int ChaveId, string Valor, int Id, string Chave);
}
