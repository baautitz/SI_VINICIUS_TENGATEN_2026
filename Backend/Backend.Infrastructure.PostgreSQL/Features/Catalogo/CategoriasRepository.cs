using System.Linq;
using Backend.Core.Common.Results;
using Backend.Core.Features.Catalogo.Entities;
using Backend.Core.Features.Catalogo.Repositories;
using Backend.Infrastructure.PostgreSQL.Common;
using Dapper;
using Npgsql;

namespace Backend.Infrastructure.PostgreSQL.Features.Catalogo;

public class CategoriasRepository : ICategoriasRepository
{
    private readonly DbSession _session;

    public CategoriasRepository(DbSession session)
    {
        _session = session;
    }

    public async Task<ResultadoPaginado<Categorias>> ObterCategorias(int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sql = @"
            SELECT COUNT(*) FROM categorias;

            SELECT id, categoria, descricao, ativo
            FROM categorias
            ORDER BY id DESC
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql,
            new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction
        );

        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<Categorias>();

        return new ResultadoPaginado<Categorias>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<Categorias?> ObterCategoriaPorId(int id)
    {
        const string sql = "SELECT id, categoria, descricao, ativo FROM categorias WHERE id = @Id;";

        return await _session.Connection.QuerySingleOrDefaultAsync<Categorias>(
            sql,
            new { Id = id },
            transaction: _session.Transaction
        );
    }

    public async Task<Categorias> CriarCategoria(Categorias categoria)
    {
        try
        {
            const string sql = @"
                INSERT INTO categorias (categoria, descricao, ativo)
                VALUES (@Categoria, @Descricao, @Ativo)
                RETURNING id;";

            var idGerado = await _session.Connection.ExecuteScalarAsync<int>(
                sql,
                new { categoria.Categoria, categoria.Descricao, categoria.Ativo },
                transaction: _session.Transaction
            );

            return new Categorias(idGerado, categoria.Categoria, categoria.Descricao);
        }
        catch (PostgresException ex)
        {
            throw DbExceptionTranslator.Translate(ex);
        }
    }

    public async Task<Categorias> AtualizarCategoria(int id, Categorias categoria)
    {
        try
        {
            const string sql = @"
                UPDATE categorias
                SET categoria = @Categoria,
                    descricao = @Descricao,
                    ativo = @Ativo
                WHERE id = @Id;";

            await _session.Connection.ExecuteAsync(
                sql,
                new { Id = id, categoria.Categoria, categoria.Descricao, categoria.Ativo },
                transaction: _session.Transaction
            );

            return new Categorias(id, categoria.Categoria, categoria.Descricao);
        }
        catch (PostgresException ex)
        {
            throw DbExceptionTranslator.Translate(ex);
        }
    }

    public async Task<bool> DeletarCategoria(int id)
    {
        try
        {
            const string sql = "DELETE FROM categorias WHERE id = @Id;";

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

    public async Task<ResultadoPaginado<Categorias>> PesquisarCategorias(string termo, int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sql = @"
            SELECT COUNT(*)
            FROM categorias
            WHERE unaccent(categoria::text) ILIKE unaccent(@Termo::text) 
               OR unaccent(descricao::text) ILIKE unaccent(@Termo::text);

            SELECT id, categoria, descricao, ativo
            FROM categorias
            WHERE unaccent(categoria::text) ILIKE unaccent(@Termo::text) 
               OR unaccent(descricao::text) ILIKE unaccent(@Termo::text)
            ORDER BY id DESC
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql,
            new { Termo = $"%{termo}%", TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction
        );

        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<Categorias>();

        return new ResultadoPaginado<Categorias>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<bool> ExisteCategoria(string categoria, int? ignorarId = null)
    {
        var sql = "SELECT COUNT(1) FROM categorias WHERE unaccent(categoria::text) ILIKE unaccent(@Categoria::text)";
        if (ignorarId.HasValue)
            sql += " AND id != @IgnorarId";

        return await _session.Connection.ExecuteScalarAsync<int>(
            sql,
            new { Categoria = categoria, IgnorarId = ignorarId },
            transaction: _session.Transaction
        ) > 0;
    }
}
