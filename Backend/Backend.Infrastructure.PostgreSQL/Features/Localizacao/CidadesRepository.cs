using Backend.Core.Common;
using Backend.Core.Features.Localizacao.DTOs;
using Backend.Core.Features.Localizacao.Entities;
using Backend.Core.Features.Localizacao.Repositories;
using Backend.Infrastructure.PostgreSQL.Common;
using Dapper;

namespace Backend.Infrastructure.PostgreSQL.Features.Localizacao;

public class CidadesRepository : ICidadesRepository
{
    private readonly DbSession _session;

    public CidadesRepository(DbSession session)
    {
        _session = session;
    }

    public async Task<ResultadoPaginado<Cidades>> ObterCidades(int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string countSql = "SELECT COUNT(*) FROM cidades;";

        const string querySql = @"
            SELECT c.id AS Id, c.cidade, c.ddd,
                   e.id AS EstadoId, e.estado, e.uf,
                   p.id AS PaisId, p.pais, p.sigla_iso, p.ddi, p.moeda, p.simbolo_moeda
            FROM cidades c
            JOIN estados e ON e.id = c.estado_id
            JOIN paises p ON p.id = e.pais_id
            ORDER BY c.cidade
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        var total = await _session.Connection.ExecuteScalarAsync<int>(
            countSql, transaction: _session.Transaction);

        var itens = await _session.Connection.QueryAsync<Cidades, Estados, Paises, Cidades>(
            querySql,
            (cidade, estado, pais) =>
            {
                estado.Pais = pais;
                cidade.Estado = estado;
                return cidade;
            },
            new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction,
            splitOn: "EstadoId,PaisId"
        );

        return new ResultadoPaginado<Cidades>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<Cidades?> ObterCidadePorId(int id)
    {
        const string sql = @"
            SELECT c.id AS Id, c.cidade, c.ddd,
                   e.id AS EstadoId, e.estado, e.uf,
                   p.id AS PaisId, p.pais, p.sigla_iso, p.ddi, p.moeda, p.simbolo_moeda
            FROM cidades c
            JOIN estados e ON e.id = c.estado_id
            JOIN paises p ON p.id = e.pais_id
            WHERE c.id = @Id;";

        var result = await _session.Connection.QueryAsync<Cidades, Estados, Paises, Cidades>(
            sql,
            (cidade, estado, pais) =>
            {
                estado.Pais = pais;
                cidade.Estado = estado;
                return cidade;
            },
            new { Id = id },
            transaction: _session.Transaction,
            splitOn: "EstadoId,PaisId"
        );

        return result.SingleOrDefault();
    }

    public async Task<Cidades> CriarCidade(Cidades cidade)
    {
        const string sql = @"
            INSERT INTO cidades (cidade, ddd, estado_id)
            VALUES (@Cidade, @Ddd, @EstadoId)
            RETURNING id;";

        var idGerado = await _session.Connection.ExecuteScalarAsync<int>(
            sql,
            new { cidade.Cidade, cidade.Ddd, EstadoId = cidade.Estado.Id },
            transaction: _session.Transaction
        );

        cidade.Id = idGerado;
        return cidade;
    }

    public async Task<Cidades> AtualizarCidade(int id, Cidades cidade)
    {
        const string sql = @"
            UPDATE cidades
            SET cidade = @Cidade,
                ddd = @Ddd,
                estado_id = @EstadoId
            WHERE id = @Id;";

        await _session.Connection.ExecuteAsync(
            sql,
            new { Id = id, cidade.Cidade, cidade.Ddd, EstadoId = cidade.Estado.Id },
            transaction: _session.Transaction
        );

        cidade.Id = id;
        return cidade;
    }

    public async Task<bool> DeletarCidade(int id)
    {
        const string sql = "DELETE FROM cidades WHERE id = @Id;";

        var linhasAfetadas = await _session.Connection.ExecuteAsync(
            sql,
            new { Id = id },
            transaction: _session.Transaction
        );

        return linhasAfetadas > 0;
    }

    public async Task<ResultadoPaginado<CidadesResumo>> ObterCidadesResumo(int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sql = @"
            SELECT COUNT(*) FROM cidades;

            SELECT id, cidade, ddd
            FROM cidades
            ORDER BY cidade
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql,
            new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction
        );

        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<CidadesResumo>();

        return new ResultadoPaginado<CidadesResumo>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<ResultadoPaginado<CidadesResumo>> PesquisarCidades(string termo, int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sql = @"
            SELECT COUNT(*)
            FROM cidades
            WHERE cidade ILIKE @Termo;

            SELECT id, cidade, ddd
            FROM cidades
            WHERE cidade ILIKE @Termo
            ORDER BY cidade
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql,
            new { Termo = $"%{termo}%", TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction
        );

        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<CidadesResumo>();

        return new ResultadoPaginado<CidadesResumo>(itens, total, pagina, tamanhoDaPagina);
    }
}
