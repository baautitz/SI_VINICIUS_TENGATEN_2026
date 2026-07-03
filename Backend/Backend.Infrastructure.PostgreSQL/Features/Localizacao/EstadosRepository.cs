using System.Linq;
using Backend.Core.Common.Results;
using Backend.Core.Features.Localizacao.Entities;
using Backend.Core.Features.Localizacao.Repositories;
using Backend.Infrastructure.PostgreSQL.Common;
using Dapper;

namespace Backend.Infrastructure.PostgreSQL.Features.Localizacao;

public class EstadosRepository : IEstadosRepository
{
    private readonly DbSession _session;

    public EstadosRepository(DbSession session) => _session = session;

    public async Task<ResultadoPaginado<Estados>> ObterEstados(int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;
        const string sqlCount = "SELECT COUNT(*) FROM estados;";
        const string sqlData = @"
            SELECT e.id, e.estado, e.uf, p.id AS PaisId, p.id AS Id, p.pais, p.codigo_iso_pais, p.ddi, p.codigo_iso_moeda, p.simbolo_moeda
            FROM estados e
            INNER JOIN paises p ON p.id = e.pais_id
            ORDER BY e.id DESC
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        var total = await _session.Connection.ExecuteScalarAsync<int>(sqlCount, transaction: _session.Transaction);
        var itens = await _session.Connection.QueryAsync<Estados, Paises, Estados>(
            sqlData,
            (estado, pais) => { estado.AtualizarResultado(estado.Estado, estado.Uf, pais); return estado; },
            new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction,
            splitOn: "PaisId"
        );
        return new ResultadoPaginado<Estados>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<Estados?> ObterEstadoPorId(int id)
    {
        const string sql = @"
            SELECT e.id, e.estado, e.uf, p.id AS PaisId, p.id AS Id, p.pais, p.codigo_iso_pais, p.ddi, p.codigo_iso_moeda, p.simbolo_moeda
            FROM estados e
            INNER JOIN paises p ON p.id = e.pais_id
            WHERE e.id = @Id;";

        var result = await _session.Connection.QueryAsync<Estados, Paises, Estados>(
            sql,
            (estado, pais) => { estado.AtualizarResultado(estado.Estado, estado.Uf, pais); return estado; },
            new { Id = id },
            transaction: _session.Transaction,
            splitOn: "PaisId"
        );
        return result.SingleOrDefault();
    }

    public async Task<Estados> CriarEstado(Estados estado)
    {
        const string sql = "INSERT INTO estados (estado, uf, pais_id) VALUES (@Estado, @Uf, @PaisId) RETURNING id;";
        var id = await _session.Connection.ExecuteScalarAsync<int>(sql, new { estado.Estado, estado.Uf, PaisId = estado.Pais.Id }, transaction: _session.Transaction);
        return new Estados(id, estado.Estado, estado.Uf, estado.Pais);
    }

    public async Task<Estados> AtualizarEstado(int id, Estados estado)
    {
        const string sql = "UPDATE estados SET estado = @Estado, uf = @Uf, pais_id = @PaisId WHERE id = @Id;";
        await _session.Connection.ExecuteAsync(sql, new { Id = id, estado.Estado, estado.Uf, PaisId = estado.Pais.Id }, transaction: _session.Transaction);
        return new Estados(id, estado.Estado, estado.Uf, estado.Pais);
    }

    public async Task<bool> DeletarEstado(int id)
    {
        const string sql = "DELETE FROM estados WHERE id = @Id;";
        return await _session.Connection.ExecuteAsync(sql, new { Id = id }, transaction: _session.Transaction) > 0;
    }

    public async Task<ResultadoPaginado<Estados>> PesquisarEstados(string termo, int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;
        const string sqlCount = "SELECT COUNT(*) FROM estados WHERE estado ILIKE @Termo OR uf ILIKE @Termo;";
        const string sqlData = @"
            SELECT e.id, e.estado, e.uf, p.id AS PaisId, p.id AS Id, p.pais, p.codigo_iso_pais, p.ddi, p.codigo_iso_moeda, p.simbolo_moeda
            FROM estados e
            INNER JOIN paises p ON p.id = e.pais_id
            WHERE e.estado ILIKE @Termo OR e.uf ILIKE @Termo
            ORDER BY e.id DESC
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        var total = await _session.Connection.ExecuteScalarAsync<int>(sqlCount, new { Termo = $"%{termo}%" }, transaction: _session.Transaction);
        var itens = await _session.Connection.QueryAsync<Estados, Paises, Estados>(
            sqlData,
            (estado, pais) => { estado.AtualizarResultado(estado.Estado, estado.Uf, pais); return estado; },
            new { Termo = $"%{termo}%", TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction,
            splitOn: "PaisId"
        );
        return new ResultadoPaginado<Estados>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<bool> ExisteEstado(string uf, int paisId, int? ignorarId = null)
    {
        var sql = "SELECT COUNT(1) FROM estados WHERE uf = @Uf AND pais_id = @PaisId";
        if (ignorarId.HasValue) sql += " AND id != @IgnorarId";
        return await _session.Connection.ExecuteScalarAsync<int>(sql, new { Uf = uf, PaisId = paisId, IgnorarId = ignorarId }, transaction: _session.Transaction) > 0;
    }
}
