using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend.Core.Common.Results;
using Backend.Core.Features.Catalogo.Entities;
using Backend.Core.Features.Catalogo.Repositories;
using Backend.Infrastructure.PostgreSQL.Common;
using Dapper;
using Npgsql;

namespace Backend.Infrastructure.PostgreSQL.Features.Catalogo;

public class SkuAtributosChavesRepository : ISkuAtributosChavesRepository
{
    private readonly DbSession _session;

    public SkuAtributosChavesRepository(DbSession session)
    {
        _session = session;
    }

    public async Task<ResultadoPaginado<SkuAtributosChaves>> ObterAtributos(int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sql = @"
            SELECT COUNT(*) FROM sku_atributos_chaves;

            SELECT id, chave FROM sku_atributos_chaves
            ORDER BY chave
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql,
            new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction
        );

        var total = await multi.ReadSingleAsync<int>();
        var chaves = (await multi.ReadAsync<SkuAtributosChaves>()).ToList();

        if (chaves.Any())
        {
            var ids = chaves.Select(x => x.Id).ToArray();
            const string valuesSql = "SELECT id, chave_id AS ChaveId, valor FROM sku_atributos_valores WHERE chave_id = ANY(@Ids);";

            var valores = (await _session.Connection.QueryAsync<ValoresDto>(
                valuesSql,
                new { Ids = ids },
                transaction: _session.Transaction
            )).ToList();

            var valoresPorChave = valores.GroupBy(x => x.ChaveId).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var chave in chaves)
            {
                if (valoresPorChave.TryGetValue(chave.Id, out var subValores))
                {
                    foreach (var val in subValores)
                    {
                        chave.AdicionarValor(new SkuAtributosValores(val.Id, val.ChaveId, val.Valor));
                    }
                }
            }
        }

        return new ResultadoPaginado<SkuAtributosChaves>(chaves, total, pagina, tamanhoDaPagina);
    }

    public async Task<SkuAtributosChaves?> ObterAtributoPorId(int id)
    {
        const string sql = "SELECT id, chave FROM sku_atributos_chaves WHERE id = @Id;";
        var chave = await _session.Connection.QuerySingleOrDefaultAsync<SkuAtributosChaves>(
            sql,
            new { Id = id },
            transaction: _session.Transaction
        );

        if (chave is null) return null;

        const string valuesSql = "SELECT id, chave_id AS ChaveId, valor FROM sku_atributos_valores WHERE chave_id = @Id;";
        var valores = await _session.Connection.QueryAsync<ValoresDto>(
            valuesSql,
            new { Id = id },
            transaction: _session.Transaction
        );

        foreach (var val in valores)
        {
            chave.AdicionarValor(new SkuAtributosValores(val.Id, val.ChaveId, val.Valor));
        }

        return chave;
    }

    public async Task<SkuAtributosChaves> CriarAtributo(SkuAtributosChaves atributo)
    {
        try
        {
            const string sql = "INSERT INTO sku_atributos_chaves (chave) VALUES (@Chave) RETURNING id;";
            var idGerado = await _session.Connection.ExecuteScalarAsync<int>(
                sql,
                new { atributo.Chave },
                transaction: _session.Transaction
            );

            var chaveCriada = new SkuAtributosChaves(idGerado, atributo.Chave);

            foreach (var val in atributo.SkuAtributosValores)
            {
                const string valSql = "INSERT INTO sku_atributos_valores (chave_id, valor) VALUES (@ChaveId, @Valor) RETURNING id;";
                var valId = await _session.Connection.ExecuteScalarAsync<int>(
                    valSql,
                    new { ChaveId = idGerado, val.Valor },
                    transaction: _session.Transaction
                );
                chaveCriada.AdicionarValor(new SkuAtributosValores(valId, idGerado, val.Valor));
            }

            return chaveCriada;
        }
        catch (PostgresException ex)
        {
            throw DbExceptionTranslator.Translate(ex);
        }
    }

    public async Task<SkuAtributosChaves> AtualizarAtributo(int id, SkuAtributosChaves atributo)
    {
        try
        {
            const string sql = "UPDATE sku_atributos_chaves SET chave = @Chave WHERE id = @Id;";
            await _session.Connection.ExecuteAsync(
                sql,
                new { Id = id, atributo.Chave },
                transaction: _session.Transaction
            );

            const string deleteValSql = "DELETE FROM sku_atributos_valores WHERE chave_id = @Id;";
            await _session.Connection.ExecuteAsync(deleteValSql, new { Id = id }, transaction: _session.Transaction);

            var chaveAtualizada = new SkuAtributosChaves(id, atributo.Chave);

            foreach (var val in atributo.SkuAtributosValores)
            {
                const string valSql = "INSERT INTO sku_atributos_valores (chave_id, valor) VALUES (@ChaveId, @Valor) RETURNING id;";
                var valId = await _session.Connection.ExecuteScalarAsync<int>(
                    valSql,
                    new { ChaveId = id, val.Valor },
                    transaction: _session.Transaction
                );
                chaveAtualizada.AdicionarValor(new SkuAtributosValores(valId, id, val.Valor));
            }

            return chaveAtualizada;
        }
        catch (PostgresException ex)
        {
            throw DbExceptionTranslator.Translate(ex);
        }
    }

    public async Task<bool> DeletarAtributo(int id)
    {
        try
        {
            const string sql = "DELETE FROM sku_atributos_chaves WHERE id = @Id;";
            var linhasAfetadas = await _session.Connection.ExecuteAsync(
                sql,
                new { Id = id },
                transaction: _session.Transaction
            );
            return linhasAfetadas > 0;
        }
        catch (PostgresException ex)
        {
            throw DbExceptionTranslator.Translate(ex);
        }
    }

    public async Task<ResultadoPaginado<SkuAtributosChaves>> PesquisarAtributos(string termo, int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sql = @"
            SELECT COUNT(*)
            FROM sku_atributos_chaves
            WHERE unaccent(chave::text) ILIKE unaccent(@Termo::text);

            SELECT id, chave FROM sku_atributos_chaves
            WHERE unaccent(chave::text) ILIKE unaccent(@Termo::text)
            ORDER BY chave
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql,
            new { Termo = $"%{termo}%", TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction
        );

        var total = await multi.ReadSingleAsync<int>();
        var chaves = (await multi.ReadAsync<SkuAtributosChaves>()).ToList();

        if (chaves.Any())
        {
            var ids = chaves.Select(x => x.Id).ToArray();
            const string valuesSql = "SELECT id, chave_id AS ChaveId, valor FROM sku_atributos_valores WHERE chave_id = ANY(@Ids) ORDER BY valor;";

            var valores = (await _session.Connection.QueryAsync<ValoresDto>(
                valuesSql,
                new { Ids = ids },
                transaction: _session.Transaction
            )).ToList();

            var valoresPorChave = valores.GroupBy(x => x.ChaveId).ToDictionary(g => g.Key, g => g.ToList());

            foreach (var chave in chaves)
            {
                if (valoresPorChave.TryGetValue(chave.Id, out var subValores))
                {
                    foreach (var val in subValores)
                    {
                        chave.AdicionarValor(new SkuAtributosValores(val.Id, val.ChaveId, val.Valor));
                    }
                }
            }
        }

        return new ResultadoPaginado<SkuAtributosChaves>(chaves, total, pagina, tamanhoDaPagina);
    }

    public async Task<bool> ExisteChave(string chave, int? ignorarId = null)
    {
        var sql = "SELECT COUNT(1) FROM sku_atributos_chaves WHERE unaccent(chave::text) ILIKE unaccent(@Chave::text)";
        if (ignorarId.HasValue)
            sql += " AND id != @IgnorarId";

        return await _session.Connection.ExecuteScalarAsync<int>(
            sql,
            new { Chave = chave, IgnorarId = ignorarId },
            transaction: _session.Transaction
        ) > 0;
    }

    public async Task<List<SkuAtributosValores>> ObterValoresPorIds(IEnumerable<int> ids)
    {
        if (ids == null || !ids.Any()) return new List<SkuAtributosValores>();
        const string sql = "SELECT id, chave_id AS ChaveId, valor FROM sku_atributos_valores WHERE id = ANY(@Ids);";
        var dtos = await _session.Connection.QueryAsync<ValoresDto>(
            sql, new { Ids = ids.ToArray() }, transaction: _session.Transaction);
        return dtos.Select(d => new SkuAtributosValores(d.Id, d.ChaveId, d.Valor)).ToList();
    }

    private sealed record ValoresDto(int Id, int ChaveId, string Valor);
}
