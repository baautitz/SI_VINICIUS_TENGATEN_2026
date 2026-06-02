using Backend.Core.Common.Results;
using Backend.Core.Features.Catalogo.DTOs;
using Backend.Core.Features.Catalogo.Entities;
using Backend.Core.Features.Catalogo.Repositories;
using Backend.Infrastructure.PostgreSQL.Common;
using Dapper;

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
            ORDER BY categoria
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
        const string sql = @"
            INSERT INTO categorias (categoria, descricao, ativo)
            VALUES (@Categoria, @Descricao, @Ativo)
            RETURNING id;";

        var idGerado = await _session.Connection.ExecuteScalarAsync<int>(
            sql,
            categoria,
            transaction: _session.Transaction
        );

        return new Categorias(idGerado, categoria.Categoria, categoria.Descricao);
    }

    public async Task<Categorias> AtualizarCategoria(int id, Categorias categoria)
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

    public async Task<bool> DeletarCategoria(int id)
    {
        const string sql = "DELETE FROM categorias WHERE id = @Id;";

        var linhasAfetadas = await _session.Connection.ExecuteAsync(
            sql,
            new { Id = id },
            transaction: _session.Transaction
        );

        return linhasAfetadas > 0;
    }

    public async Task<ResultadoPaginado<CategoriasResumo>> ObterCategoriasResumo(int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sql = @"
            SELECT COUNT(*) FROM categorias;

            SELECT id, categoria, ativo
            FROM categorias
            ORDER BY categoria
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql,
            new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction
        );

        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<CategoriasResumo>();

        return new ResultadoPaginado<CategoriasResumo>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<ResultadoPaginado<CategoriasResumo>> PesquisarCategorias(string termo, int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sql = @"
            SELECT COUNT(*)
            FROM categorias
            WHERE categoria ILIKE @Termo OR descricao ILIKE @Termo;

            SELECT id, categoria, ativo
            FROM categorias
            WHERE categoria ILIKE @Termo OR descricao ILIKE @Termo
            ORDER BY categoria
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql,
            new { Termo = $"%{termo}%", TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction
        );

        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<CategoriasResumo>();

        return new ResultadoPaginado<CategoriasResumo>(itens, total, pagina, tamanhoDaPagina);
    }
}
