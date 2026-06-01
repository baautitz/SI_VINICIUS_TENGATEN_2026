using Backend.Core.Common;
using Backend.Core.Features.Localizacao.DTOs;
using Backend.Core.Features.Localizacao.Entities;
using Backend.Core.Features.Localizacao.Repositories;
using Backend.Infrastructure.PostgreSQL.Common;
using Dapper;

namespace Backend.Infrastructure.PostgreSQL.Features.Localizacao;

public class EstadosRepository : IEstadosRepository
{
    private readonly DbSession _session;

    public EstadosRepository(DbSession session)
    {
        _session = session;
    }

    public async Task<ResultadoPaginado<EstadoResumoDto>> ObterEstados(string? search, int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        string sql;
        object parametros;

        if (string.IsNullOrWhiteSpace(search))
        {
            sql = @"
                SELECT COUNT(*) FROM estados;
                SELECT e.id, e.estado AS Estado, e.uf AS Uf, p.id AS PaisId, p.pais AS PaisNome
                FROM estados e
                JOIN paises p ON p.id = e.pais_id
                ORDER BY e.estado LIMIT @TamanhoDaPagina OFFSET @Offset;";
            parametros = new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset };
        }
        else
        {
            sql = @"
                SELECT COUNT(*) FROM estados WHERE unaccent(estado::text) ILIKE unaccent(@Termo::text) OR unaccent(uf::text) ILIKE unaccent(@Termo::text);
                SELECT e.id, e.estado AS Estado, e.uf AS Uf, p.id AS PaisId, p.pais AS PaisNome
                FROM estados e
                JOIN paises p ON p.id = e.pais_id
                WHERE unaccent(e.estado::text) ILIKE unaccent(@Termo::text) OR unaccent(e.uf::text) ILIKE unaccent(@Termo::text)
                ORDER BY e.estado LIMIT @TamanhoDaPagina OFFSET @Offset;";
            parametros = new { Termo = "%" + search + "%", TamanhoDaPagina = tamanhoDaPagina, Offset = offset };
        }

        using var multi = await _session.Connection.QueryMultipleAsync(sql, parametros, transaction: _session.Transaction);
        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<EstadoResumoDto>();
        return new ResultadoPaginado<EstadoResumoDto>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<Estados?> ObterEstadoPorId(int id)
    {
        const string sql = @"
            SELECT e.id, e.estado, e.uf,
                   p.id AS pais_id, p.id, p.pais, p.sigla_iso, p.ddi, p.moeda, p.simbolo_moeda
            FROM estados e
            JOIN paises p ON p.id = e.pais_id
            WHERE e.id = @Id;";
        var result = await _session.Connection.QueryAsync<Estados, Paises, Estados>(
            sql,
            (estado, pais) => {
                estado.AtualizarResultado(estado.Estado, estado.Uf, pais);
                return estado;
            },
            new { Id = id },
            transaction: _session.Transaction,
            splitOn: "pais_id"
        );
        return result.SingleOrDefault();
    }

    public async Task<EstadoResumoDto?> ObterEstadoDetalhePorId(int id)
    {
        const string sql = @"
            SELECT e.id, e.estado AS Estado, e.uf AS Uf, p.id AS PaisId, p.pais AS PaisNome
            FROM estados e
            JOIN paises p ON p.id = e.pais_id
            WHERE e.id = @Id;";
        return await _session.Connection.QuerySingleOrDefaultAsync<EstadoResumoDto>(
            sql, new { Id = id }, transaction: _session.Transaction);
    }

    public Task<Estados> CriarEstado(Estados estado)
    {
        return DbSessionExtensions.ExecuteWithConflictCheckAsync(async () =>
        {
            const string sql = "INSERT INTO estados (estado, uf, pais_id) VALUES (@Estado, @Uf, @PaisId) RETURNING id;";
            var idGerado = await _session.Connection.ExecuteScalarAsync<int>(sql, new { estado.Estado, estado.Uf, PaisId = estado.Pais.Id }, transaction: _session.Transaction);
            return new Estados(idGerado, estado.Estado, estado.Uf, estado.Pais);
        });
    }

    public Task<Estados> AtualizarEstado(int id, Estados estado)
    {
        return DbSessionExtensions.ExecuteWithConflictCheckAsync(async () =>
        {
            const string sql = "UPDATE estados SET estado = @Estado, uf = @Uf, pais_id = @PaisId WHERE id = @Id;";
            await _session.Connection.ExecuteAsync(sql, new { Id = id, estado.Estado, estado.Uf, PaisId = estado.Pais.Id }, transaction: _session.Transaction);
            return new Estados(id, estado.Estado, estado.Uf, estado.Pais);
        });
    }

    public async Task<bool> ExisteEstado(int paisId, string uf, int? ignorarId = null)
    {
        var sql = "SELECT COUNT(1) FROM estados WHERE pais_id = @PaisId AND uf = @Uf";
        if (ignorarId.HasValue) sql += " AND id != @IgnorarId";
        return await _session.Connection.ExecuteScalarAsync<int>(sql, new { PaisId = paisId, Uf = uf, IgnorarId = ignorarId }, transaction: _session.Transaction) > 0;
    }

    public async Task<bool> DeletarEstado(int id)
    {
        const string sql = "DELETE FROM estados WHERE id = @Id;";
        var rows = await _session.Connection.ExecuteAsync(sql, new { Id = id }, transaction: _session.Transaction);
        return rows > 0;
    }
}