using System.Linq;
using Backend.Core.Common.Results;
using Backend.Core.Features.Catalogo.Entities;
using Backend.Core.Features.Catalogo.Repositories;
using Backend.Infrastructure.PostgreSQL.Common;
using Dapper;
using Npgsql;

namespace Backend.Infrastructure.PostgreSQL.Features.Catalogo;

public class UnidadesMedidaRepository : IUnidadesMedidaRepository
{
    private readonly DbSession _session;

    public UnidadesMedidaRepository(DbSession session)
    {
        _session = session;
    }

    public async Task<ResultadoPaginado<UnidadesMedida>> ObterUnidadesMedida(int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sql = @"
            SELECT COUNT(*) FROM unidades_medida;
            SELECT id, sigla, descricao, categoria, permite_decimais AS PermiteDecimais, ativo
            FROM unidades_medida
            ORDER BY descricao
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(sql, new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset }, transaction: _session.Transaction);
        var total = await multi.ReadSingleAsync<int>();
        var items = (await multi.ReadAsync<UnidadesMedida>()).ToList();

        return new ResultadoPaginado<UnidadesMedida>(items, total, pagina, tamanhoDaPagina);
    }

    public async Task<UnidadesMedida?> ObterUnidadeMedidaPorId(int id)
    {
        const string sql = "SELECT id, sigla, descricao, categoria, permite_decimais AS PermiteDecimais, ativo FROM unidades_medida WHERE id = @Id;";
        return await _session.Connection.QuerySingleOrDefaultAsync<UnidadesMedida>(sql, new { Id = id }, transaction: _session.Transaction);
    }

    public async Task<UnidadesMedida> CriarUnidadeMedida(UnidadesMedida unidadeMedida)
    {
        try
        {
            const string sql = @"
                INSERT INTO unidades_medida (sigla, descricao, categoria, permite_decimais, ativo)
                VALUES (@Sigla, @Descricao, @Categoria, @PermiteDecimais, @Ativo)
                RETURNING id;";
            var idGerado = await _session.Connection.ExecuteScalarAsync<int>(
                sql,
                new { unidadeMedida.Sigla, unidadeMedida.Descricao, unidadeMedida.Categoria, unidadeMedida.PermiteDecimais, unidadeMedida.Ativo },
                transaction: _session.Transaction
            );
            var novaUnidade = new UnidadesMedida(unidadeMedida.Sigla, unidadeMedida.Descricao, unidadeMedida.Categoria, unidadeMedida.PermiteDecimais, unidadeMedida.Ativo) { Id = idGerado };
            return novaUnidade;
            }
            catch (PostgresException ex)
            {
            throw DbExceptionTranslator.Translate(ex);
            }
            }

            public async Task<UnidadesMedida> AtualizarUnidadeMedida(int id, UnidadesMedida unidadeMedida)
            {
            try
            {
            const string sql = @"
                UPDATE unidades_medida
                SET sigla = @Sigla,
                    descricao = @Descricao,
                    categoria = @Categoria,
                    permite_decimais = @PermiteDecimais,
                    ativo = @Ativo
                WHERE id = @Id;";
            await _session.Connection.ExecuteAsync(
                sql,
                new { Id = id, unidadeMedida.Sigla, unidadeMedida.Descricao, unidadeMedida.Categoria, unidadeMedida.PermiteDecimais, unidadeMedida.Ativo },
                transaction: _session.Transaction
            );
            unidadeMedida.Id = id;
            return unidadeMedida;
        }
        catch (PostgresException ex)
        {
            throw DbExceptionTranslator.Translate(ex);
        }
    }

    public async Task<bool> DeletarUnidadeMedida(int id)
    {
        try
        {
            const string sql = "DELETE FROM unidades_medida WHERE id = @Id;";
            var rows = await _session.Connection.ExecuteAsync(sql, new { Id = id }, transaction: _session.Transaction);
            return rows > 0;
        }
        catch (PostgresException ex)
        {
            throw DbExceptionTranslator.Translate(ex);
        }
    }

    public async Task<ResultadoPaginado<UnidadesMedida>> PesquisarUnidadesMedida(string termo, int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sql = @"
            SELECT COUNT(*) FROM unidades_medida
            WHERE unaccent(sigla::text) ILIKE unaccent(@Termo::text)
               OR unaccent(descricao::text) ILIKE unaccent(@Termo::text)
               OR unaccent(categoria::text) ILIKE unaccent(@Termo::text);

            SELECT id, sigla, descricao, categoria, permite_decimais AS PermiteDecimais, ativo
            FROM unidades_medida
            WHERE unaccent(sigla::text) ILIKE unaccent(@Termo::text)
               OR unaccent(descricao::text) ILIKE unaccent(@Termo::text)
               OR unaccent(categoria::text) ILIKE unaccent(@Termo::text)
            ORDER BY descricao
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql,
            new { Termo = $"%{termo}%", TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction
        );
        var total = await multi.ReadSingleAsync<int>();
        var items = (await multi.ReadAsync<UnidadesMedida>()).ToList();

        return new ResultadoPaginado<UnidadesMedida>(items, total, pagina, tamanhoDaPagina);
    }

    public async Task<bool> ExisteSigla(string sigla, int? ignorarId = null)
    {
        var sql = "SELECT COUNT(1) FROM unidades_medida WHERE sigla = @Sigla";
        if (ignorarId.HasValue)
            sql += " AND id != @IgnorarId";

        return await _session.Connection.ExecuteScalarAsync<int>(
            sql,
            new { Sigla = sigla, IgnorarId = ignorarId },
            transaction: _session.Transaction
        ) > 0;
    }
}
