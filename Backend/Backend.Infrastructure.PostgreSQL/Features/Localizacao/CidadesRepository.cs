using System.Linq;
using Backend.Core.Common.Results;
using Backend.Core.Features.Localizacao.Entities;
using Backend.Core.Features.Localizacao.Repositories;
using Backend.Infrastructure.PostgreSQL.Common;
using Dapper;

namespace Backend.Infrastructure.PostgreSQL.Features.Localizacao;

public class CidadesRepository : ICidadesRepository
{
    private readonly DbSession _session;

    public CidadesRepository(DbSession session) => _session = session;

    public async Task<ResultadoPaginado<Cidades>> ObterCidades(int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;
        const string sqlCount = "SELECT COUNT(*) FROM cidades;";
        const string sqlData = @"
            SELECT ci.id, ci.cidade, ci.ddd, st.id AS EstadoId, st.id AS Id, st.estado, st.uf, p.id AS PaisId, p.id AS Id, p.pais, p.sigla_iso, p.ddi, p.moeda, p.simbolo_moeda
            FROM cidades ci
            INNER JOIN estados st ON st.id = ci.estado_id
            INNER JOIN paises p ON p.id = st.pais_id
            ORDER BY ci.cidade
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        var total = await _session.Connection.ExecuteScalarAsync<int>(sqlCount, transaction: _session.Transaction);
        var itens = await _session.Connection.QueryAsync<Cidades, Estados, Paises, Cidades>(
            sqlData,
            (cidade, estado, pais) => { estado.AtualizarResultado(estado.Estado, estado.Uf, pais); cidade.AtualizarResultado(cidade.Cidade, cidade.Ddd, estado); return cidade; },
            new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction,
            splitOn: "EstadoId,PaisId"
        );
        return new ResultadoPaginado<Cidades>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<Cidades?> ObterCidadePorId(int id)
    {
        const string sql = @"
            SELECT ci.id, ci.cidade, ci.ddd, st.id AS EstadoId, st.id AS Id, st.estado, st.uf, p.id AS PaisId, p.id AS Id, p.pais, p.sigla_iso, p.ddi, p.moeda, p.simbolo_moeda
            FROM cidades ci
            INNER JOIN estados st ON st.id = ci.estado_id
            INNER JOIN paises p ON p.id = st.pais_id
            WHERE ci.id = @Id;";

        var result = await _session.Connection.QueryAsync<Cidades, Estados, Paises, Cidades>(
            sql,
            (cidade, estado, pais) => { estado.AtualizarResultado(estado.Estado, estado.Uf, pais); cidade.AtualizarResultado(cidade.Cidade, cidade.Ddd, estado); return cidade; },
            new { Id = id },
            transaction: _session.Transaction,
            splitOn: "EstadoId,PaisId"
        );
        return result.SingleOrDefault();
    }

    public async Task<Cidades> CriarCidade(Cidades cidade)
    {
        const string sql = "INSERT INTO cidades (cidade, ddd, estado_id) VALUES (@Cidade, @Ddd, @EstadoId) RETURNING id;";
        var id = await _session.Connection.ExecuteScalarAsync<int>(sql, new { cidade.Cidade, cidade.Ddd, EstadoId = cidade.Estado.Id }, transaction: _session.Transaction);
        return new Cidades(id, cidade.Cidade, cidade.Ddd, cidade.Estado);
    }

    public async Task<Cidades> AtualizarCidade(int id, Cidades cidade)
    {
        const string sql = "UPDATE cidades SET cidade = @Cidade, ddd = @Ddd, estado_id = @EstadoId WHERE id = @Id;";
        await _session.Connection.ExecuteAsync(sql, new { Id = id, cidade.Cidade, cidade.Ddd, EstadoId = cidade.Estado.Id }, transaction: _session.Transaction);
        return new Cidades(id, cidade.Cidade, cidade.Ddd, cidade.Estado);
    }

    public async Task<bool> DeletarCidade(int id)
    {
        const string sql = "DELETE FROM cidades WHERE id = @Id;";
        return await _session.Connection.ExecuteAsync(sql, new { Id = id }, transaction: _session.Transaction) > 0;
    }

    public async Task<ResultadoPaginado<Cidades>> PesquisarCidades(string termo, int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;
        const string sqlCount = "SELECT COUNT(*) FROM cidades WHERE cidade ILIKE @Termo OR ddd ILIKE @Termo;";
        const string sqlData = @"
            SELECT ci.id, ci.cidade, ci.ddd, st.id AS EstadoId, st.id AS Id, st.estado, st.uf, p.id AS PaisId, p.id AS Id, p.pais, p.sigla_iso, p.ddi, p.moeda, p.simbolo_moeda
            FROM cidades ci
            INNER JOIN estados st ON st.id = ci.estado_id
            INNER JOIN paises p ON p.id = st.pais_id
            WHERE ci.cidade ILIKE @Termo OR ci.ddd ILIKE @Termo
            ORDER BY ci.cidade
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        var total = await _session.Connection.ExecuteScalarAsync<int>(sqlCount, new { Termo = $"%{termo}%" }, transaction: _session.Transaction);
        var itens = await _session.Connection.QueryAsync<Cidades, Estados, Paises, Cidades>(
            sqlData,
            (cidade, estado, pais) => { estado.AtualizarResultado(estado.Estado, estado.Uf, pais); cidade.AtualizarResultado(cidade.Cidade, cidade.Ddd, estado); return cidade; },
            new { Termo = $"%{termo}%", TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction,
            splitOn: "EstadoId,PaisId"
        );
        return new ResultadoPaginado<Cidades>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<bool> ExisteCidade(string cidade, int estadoId, int? ignorarId = null)
    {
        var sql = "SELECT COUNT(1) FROM cidades WHERE cidade = @Cidade AND estado_id = @EstadoId";
        if (ignorarId.HasValue) sql += " AND id != @IgnorarId";
        return await _session.Connection.ExecuteScalarAsync<int>(sql, new { Cidade = cidade, EstadoId = estadoId, IgnorarId = ignorarId }, transaction: _session.Transaction) > 0;
    }
}
