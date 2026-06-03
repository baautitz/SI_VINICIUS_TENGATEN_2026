using Backend.Core.Common.Results;
using Backend.Core.Features.Catalogo.DTOs;
using Backend.Core.Features.Catalogo.Entities;
using Backend.Core.Features.Catalogo.Repositories;
using Backend.Infrastructure.PostgreSQL.Common;
using Dapper;
using Npgsql;

namespace Backend.Infrastructure.PostgreSQL.Features.Catalogo;

public class MarcasRepository : IMarcasRepository
{
    private readonly DbSession _session;

    public MarcasRepository(DbSession session)
    {
        _session = session;
    }

    public async Task<ResultadoPaginado<Marcas>> ObterMarcas(int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sql = @"
            SELECT COUNT(*) FROM marcas;

            SELECT id, marca, descricao, ativo
            FROM marcas
            ORDER BY marca
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql,
            new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction
        );

        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<Marcas>();

        return new ResultadoPaginado<Marcas>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<Marcas?> ObterMarcaPorId(int id)
    {
        const string sql = "SELECT id, marca, descricao, ativo FROM marcas WHERE id = @Id;";

        return await _session.Connection.QuerySingleOrDefaultAsync<Marcas>(
            sql,
            new { Id = id },
            transaction: _session.Transaction
        );
    }

    public async Task<Marcas> CriarMarca(Marcas marca)
    {
        try
        {
            const string sql = @"
                INSERT INTO marcas (marca, descricao, ativo)
                VALUES (@Marca, @Descricao, @Ativo)
                RETURNING id;";

            var idGerado = await _session.Connection.ExecuteScalarAsync<int>(
                sql,
                new { marca.Marca, marca.Descricao, marca.Ativo },
                transaction: _session.Transaction
            );

            return new Marcas(idGerado, marca.Marca, marca.Descricao);
        }
        catch (PostgresException ex)
        {
            throw DbExceptionTranslator.Translate(ex);
        }
    }

    public async Task<Marcas> AtualizarMarca(int id, Marcas marca)
    {
        try
        {
            const string sql = @"
                UPDATE marcas
                SET marca = @Marca,
                    descricao = @Descricao,
                    ativo = @Ativo
                WHERE id = @Id;";

            await _session.Connection.ExecuteAsync(
                sql,
                new { Id = id, marca.Marca, marca.Descricao, marca.Ativo },
                transaction: _session.Transaction
            );

            return new Marcas(id, marca.Marca, marca.Descricao);
        }
        catch (PostgresException ex)
        {
            throw DbExceptionTranslator.Translate(ex);
        }
    }

    public async Task<bool> DeletarMarca(int id)
    {
        try
        {
            const string sql = "DELETE FROM marcas WHERE id = @Id;";

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

    public async Task<ResultadoPaginado<MarcasResumo>> ObterMarcasResumo(int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sql = @"
            SELECT COUNT(*) FROM marcas;

            SELECT id, marca, ativo
            FROM marcas
            ORDER BY marca
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql,
            new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction
        );

        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<MarcasResumo>();

        return new ResultadoPaginado<MarcasResumo>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<ResultadoPaginado<MarcasResumo>> PesquisarMarcas(string termo, int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sql = @"
            SELECT COUNT(*)
            FROM marcas
            WHERE unaccent(marca::text) ILIKE unaccent(@Termo::text) 
               OR unaccent(descricao::text) ILIKE unaccent(@Termo::text);

            SELECT id, marca, ativo
            FROM marcas
            WHERE unaccent(marca::text) ILIKE unaccent(@Termo::text) 
               OR unaccent(descricao::text) ILIKE unaccent(@Termo::text)
            ORDER BY marca
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql,
            new { Termo = $"%{termo}%", TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction
        );

        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<MarcasResumo>();

        return new ResultadoPaginado<MarcasResumo>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<bool> ExisteMarca(string marca, int? ignorarId = null)
    {
        var sql = "SELECT COUNT(1) FROM marcas WHERE unaccent(marca::text) ILIKE unaccent(@Marca::text)";
        if (ignorarId.HasValue)
            sql += " AND id != @IgnorarId";

        return await _session.Connection.ExecuteScalarAsync<int>(
            sql,
            new { Marca = marca, IgnorarId = ignorarId },
            transaction: _session.Transaction
        ) > 0;
    }
}
