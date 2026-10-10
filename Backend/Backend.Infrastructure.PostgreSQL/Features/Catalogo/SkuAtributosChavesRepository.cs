using System;
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
            ORDER BY id DESC
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

            var valores = (await _session.Connection.QueryAsync<ValoresDbRow>(
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
        var valores = await _session.Connection.QueryAsync<ValoresDbRow>(
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

            // Nunca apaga tudo e reinsere: valor com Id mantém o registro (e os vínculos com SKUs), mesmo renomeado.
            const string existentesSql = "SELECT id, valor FROM sku_atributos_valores WHERE chave_id = @Id;";
            var existentes = (await _session.Connection.QueryAsync<(int Id, string Valor)>(
                existentesSql, new { Id = id }, transaction: _session.Transaction)).ToList();

            var desejados = atributo.SkuAtributosValores.ToList();
            var idsDesejados = desejados.Where(d => d.Id > 0).Select(d => d.Id).ToHashSet();

            // O serviço já recusa remover valor com SKU; a FK (RESTRICT) é a segunda barreira.
            foreach (var removido in existentes.Where(e => !idsDesejados.Contains(e.Id)))
            {
                await _session.Connection.ExecuteAsync(
                    "DELETE FROM sku_atributos_valores WHERE id = @ValorId;",
                    new { ValorId = removido.Id }, transaction: _session.Transaction);
            }

            // Renomeação em duas fases: trocar nomes entre valores não esbarra na unicidade (chave, valor).
            var renomeados = desejados
                .Where(d => d.Id > 0)
                .Join(existentes, d => d.Id, e => e.Id, (d, e) => (d.Id, Novo: d.Valor, Atual: e.Valor))
                .Where(r => !string.Equals(r.Novo, r.Atual, StringComparison.Ordinal))
                .ToList();
            foreach (var r in renomeados)
            {
                await _session.Connection.ExecuteAsync(
                    "UPDATE sku_atributos_valores SET valor = @Temp WHERE id = @ValorId;",
                    new { Temp = $"__renomeando_{r.Id}", ValorId = r.Id }, transaction: _session.Transaction);
            }
            foreach (var r in renomeados)
            {
                await _session.Connection.ExecuteAsync(
                    "UPDATE sku_atributos_valores SET valor = @Novo WHERE id = @ValorId;",
                    new { r.Novo, ValorId = r.Id }, transaction: _session.Transaction);
            }

            var chaveAtualizada = new SkuAtributosChaves(id, atributo.Chave);
            foreach (var desejado in desejados)
            {
                var valorId = desejado.Id;
                if (valorId <= 0)
                {
                    valorId = await _session.Connection.ExecuteScalarAsync<int>(
                        "INSERT INTO sku_atributos_valores (chave_id, valor) VALUES (@ChaveId, @Valor) RETURNING id;",
                        new { ChaveId = id, desejado.Valor },
                        transaction: _session.Transaction
                    );
                }
                chaveAtualizada.AdicionarValor(new SkuAtributosValores(valorId, id, desejado.Valor));
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
            WHERE unaccent(id::text) ILIKE unaccent(@Termo::text) OR unaccent(chave::text) ILIKE unaccent(@Termo::text);

            SELECT id, chave FROM sku_atributos_chaves
            WHERE unaccent(id::text) ILIKE unaccent(@Termo::text) OR unaccent(chave::text) ILIKE unaccent(@Termo::text)
            ORDER BY id DESC
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

            var valores = (await _session.Connection.QueryAsync<ValoresDbRow>(
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
        var rows = await _session.Connection.QueryAsync<ValoresDbRow>(
            sql, new { Ids = ids.ToArray() }, transaction: _session.Transaction);
        return rows.Select(d => new SkuAtributosValores(d.Id, d.ChaveId, d.Valor)).ToList();
    }

    public async Task<IReadOnlyDictionary<int, int>> ContarSkusPorValor(IEnumerable<int> valorIds)
    {
        var ids = valorIds.ToArray();
        if (ids.Length == 0) return new Dictionary<int, int>();

        const string sql = @"
            SELECT valor_id AS ValorId, COUNT(DISTINCT sku) AS Total
            FROM skus_atributos_valores_relacionamento
            WHERE valor_id = ANY(@Ids)
            GROUP BY valor_id;";
        var rows = await _session.Connection.QueryAsync<(int ValorId, int Total)>(
            sql, new { Ids = ids }, transaction: _session.Transaction);
        return rows.ToDictionary(r => r.ValorId, r => r.Total);
    }

    public async Task<int> ContarSkusPorChave(int chaveId)
    {
        const string sql = @"
            SELECT COUNT(DISTINCT r.sku)
            FROM skus_atributos_valores_relacionamento r
            JOIN sku_atributos_valores v ON v.id = r.valor_id
            WHERE v.chave_id = @ChaveId;";
        return await _session.Connection.ExecuteScalarAsync<int>(
            sql, new { ChaveId = chaveId }, transaction: _session.Transaction);
    }

    private sealed record ValoresDbRow(int Id, int ChaveId, string Valor);
}
