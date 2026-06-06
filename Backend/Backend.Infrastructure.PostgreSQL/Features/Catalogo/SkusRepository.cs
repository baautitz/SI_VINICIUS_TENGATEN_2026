using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend.Core.Common.Exceptions;
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
            SELECT sku, gtin_ean AS GtinEan, preco AS Preco, estoque AS Estoque, ativo AS Ativo, custo_medio AS CustoMedio, custo_ultima_compra AS CustoUltimaCompra
            FROM skus
            WHERE produto_id = @ProdutoId
            ORDER BY sku;";

        const string atributosSql = @"
            SELECT savr.sku AS Sku, sav.chave_id AS ChaveId, sav.valor AS Valor,
                   sav.id AS Id, sak.chave AS Chave
            FROM skus_atributos_valores_relacionamento savr
            JOIN sku_atributos_valores sav ON sav.id = savr.valor_id
            JOIN sku_atributos_chaves sak ON sak.id = sav.chave_id
            WHERE savr.sku IN (SELECT sku FROM skus WHERE produto_id = @ProdutoId);";

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
                        var atributo = BuildAtributo(atributoDto);
                        sku.AdicionarAtributo(atributo);
                    }
                }
            }
        }

        return new ResultadoPaginado<Skus>(skus, total, 1, total > 0 ? total : 1);
    }

    public async Task<Skus?> ObterSkuPorSku(string sku)
    {
        const string skuSql = "SELECT sku, gtin_ean AS GtinEan, preco AS Preco, estoque AS Estoque, ativo AS Ativo, custo_medio AS CustoMedio, custo_ultima_compra AS CustoUltimaCompra FROM skus WHERE sku = @Sku OR gtin_ean = @Sku;";
 
        const string atributosSql = @"
            SELECT savr.sku AS Sku, sav.chave_id AS ChaveId, sav.valor AS Valor,
                   sav.id AS Id, sak.chave AS Chave
            FROM skus_atributos_valores_relacionamento savr
            JOIN sku_atributos_valores sav ON sav.id = savr.valor_id
            JOIN sku_atributos_chaves sak ON sak.id = sav.chave_id
            WHERE savr.sku = @Sku;";
 
        var skuDto = await _session.Connection.QueryFirstOrDefaultAsync<SkuDto>(
            skuSql, new { Sku = sku }, transaction: _session.Transaction);
 
        if (skuDto is null) return null;
 
        var skuEntity = BuildSku(skuDto);
 
        var atributos = await _session.Connection.QueryAsync<AtributoDto>(
            atributosSql,
            new { Sku = skuDto.Sku },
            transaction: _session.Transaction);
 
        foreach (var atributoDto in atributos)
        {
            var atributo = BuildAtributo(atributoDto);
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

        await InserirAtributos(skuData.Sku, skuData.SkuAtributosValores);

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

        await ReplacerAtributos(sku, skuData.SkuAtributosValores);

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
        catch (Npgsql.PostgresException ex)
        {
            throw DbExceptionTranslator.Translate(ex);
        }
        catch (Exception ex)
        {
            throw new ConflictException($"DEBUG - Tipo: {ex.GetType().Name}, Mensagem: {ex.Message}");
        }
    }

    public async Task<ResultadoPaginado<SkusResumo>> PesquisarSkus(string termo, int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sql = @"
            SELECT COUNT(*)
            FROM skus s
            JOIN produtos p ON p.id = s.produto_id
            WHERE s.sku ILIKE @Termo OR s.gtin_ean ILIKE @Termo OR p.produto ILIKE @Termo;

            SELECT s.sku, s.gtin_ean AS GtinEan, s.preco AS Preco, s.estoque AS Estoque, s.ativo AS Ativo, s.custo_medio AS CustoMedio, s.custo_ultima_compra AS CustoUltimaCompra, um.permite_decimais AS PermiteDecimais, p.id AS ProdutoId, p.produto AS ProdutoNome, um.sigla AS UnidadeMedidaSigla
            FROM skus s
            JOIN produtos p ON p.id = s.produto_id
            JOIN unidades_medida um ON um.id = p.unidade_medida_id
            WHERE s.sku ILIKE @Termo OR s.gtin_ean ILIKE @Termo OR p.produto ILIKE @Termo
            ORDER BY s.sku
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql,
            new { Termo = $"%{termo}%", TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction);

        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<SkusResumo>();

        return new ResultadoPaginado<SkusResumo>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<SkusResumo?> ObterResumoPorSku(string sku)
    {
        const string sql = @"
            SELECT s.sku, s.gtin_ean AS GtinEan, s.preco AS Preco, s.estoque AS Estoque, s.ativo AS Ativo, s.custo_medio AS CustoMedio, s.custo_ultima_compra AS CustoUltimaCompra, um.permite_decimais AS PermiteDecimais, p.id AS ProdutoId, p.produto AS ProdutoNome, um.sigla AS UnidadeMedidaSigla
            FROM skus s
            JOIN produtos p ON p.id = s.produto_id
            JOIN unidades_medida um ON um.id = p.unidade_medida_id
            WHERE s.sku = @Sku OR s.gtin_ean = @Sku;";

        return await _session.Connection.QueryFirstOrDefaultAsync<SkusResumo>(
            sql, new { Sku = sku }, transaction: _session.Transaction);
    }

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

    private static Skus BuildSku(SkuDto dto)
    {
        return new Skus(dto.Sku, dto.Preco, dto.Estoque, dto.Ativo, dto.GtinEan, dto.CustoMedio, dto.CustoUltimaCompra);
    }

    private static SkuAtributosValores BuildAtributo(AtributoDto dto)
    {
        return new SkuAtributosValores(dto.Id, dto.ChaveId, dto.Valor);
    }

    private sealed record SkuDto(string Sku, string? GtinEan, decimal Preco, decimal Estoque, bool Ativo, decimal CustoMedio, decimal CustoUltimaCompra);
    private sealed record AtributoDto(string Sku, int ChaveId, string Valor, int Id, string Chave);
}
