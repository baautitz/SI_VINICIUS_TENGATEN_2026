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

    public async Task<ResultadoPaginado<Estados>> ObterEstados(int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string countSql = "SELECT COUNT(*) FROM estados;";

        const string querySql = @"
            SELECT e.id, e.estado, e.uf,
                   p.id, p.pais, p.sigla_iso, p.ddi, p.moeda, p.simbolo_moeda
            FROM estados e
            JOIN paises p ON p.id = e.pais_id
            ORDER BY e.estado
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        var total = await _session.Connection.ExecuteScalarAsync<int>(
            countSql, transaction: _session.Transaction);

        var itens = await _session.Connection.QueryAsync<Estados, Paises, Estados>(
            querySql,
            (estado, pais) =>
            {
                estado.Pais = pais;
                return estado;
            },
            new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction,
            splitOn: "id"
        );

        return new ResultadoPaginado<Estados>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<Estados?> ObterEstadoPorId(int id)
    {
        const string sql = @"
            SELECT e.id, e.estado, e.uf,
                   p.id, p.pais, p.sigla_iso, p.ddi, p.moeda, p.simbolo_moeda
            FROM estados e
            JOIN paises p ON p.id = e.pais_id
            WHERE e.id = @Id;";

        var result = await _session.Connection.QueryAsync<Estados, Paises, Estados>(
            sql,
            (estado, pais) =>
            {
                estado.Pais = pais;
                return estado;
            },
            new { Id = id },
            transaction: _session.Transaction,
            splitOn: "id"
        );

        return result.SingleOrDefault();
    }

    public async Task<Estados> CriarEstado(Estados estado)
    {
        const string sql = @"
            INSERT INTO estados (estado, uf, pais_id)
            VALUES (@Estado, @Uf, @PaisId)
            RETURNING id;";

        var idGerado = await _session.Connection.ExecuteScalarAsync<int>(
            sql,
            new { estado.Estado, estado.Uf, PaisId = estado.Pais.Id },
            transaction: _session.Transaction
        );

        estado.Id = idGerado;
        return estado;
    }

    public async Task<Estados> AtualizarEstado(int id, Estados estado)
    {
        const string sql = @"
            UPDATE estados
            SET estado = @Estado,
                uf = @Uf,
                pais_id = @PaisId
            WHERE id = @Id;";

        await _session.Connection.ExecuteAsync(
            sql,
            new { Id = id, estado.Estado, estado.Uf, PaisId = estado.Pais.Id },
            transaction: _session.Transaction
        );

        estado.Id = id;
        return estado;
    }

    public async Task<bool> DeletarEstado(int id)
    {
        const string sql = "DELETE FROM estados WHERE id = @Id;";

        var linhasAfetadas = await _session.Connection.ExecuteAsync(
            sql,
            new { Id = id },
            transaction: _session.Transaction
        );

        return linhasAfetadas > 0;
    }

    public async Task<ResultadoPaginado<EstadosResumo>> ObterEstadosResumo(int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sql = @"
            SELECT COUNT(*) FROM estados;

            SELECT id, estado, uf
            FROM estados
            ORDER BY estado
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql,
            new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction
        );

        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<EstadosResumo>();

        return new ResultadoPaginado<EstadosResumo>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<ResultadoPaginado<EstadosResumo>> PesquisarEstados(string termo, int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sql = @"
            SELECT COUNT(*)
            FROM estados
            WHERE estado ILIKE @Termo OR uf ILIKE @Termo;

            SELECT id, estado, uf
            FROM estados
            WHERE estado ILIKE @Termo OR uf ILIKE @Termo
            ORDER BY estado
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql,
            new { Termo = $"%{termo}%", TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction
        );

        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<EstadosResumo>();

        return new ResultadoPaginado<EstadosResumo>(itens, total, pagina, tamanhoDaPagina);
    }
}
