using System.Linq;
using Backend.Core.Common.Results;
using Backend.Core.Features.Localizacao.Entities;
using Backend.Core.Features.Localizacao.Repositories;
using Backend.Infrastructure.PostgreSQL.Common;
using Dapper;

namespace Backend.Infrastructure.PostgreSQL.Features.Localizacao;

public class BairrosRepository : IBairrosRepository
{
    private readonly DbSession _session;

    public BairrosRepository(DbSession session) => _session = session;

    public async Task<ResultadoPaginado<Bairros>> ObterBairros(int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;
        const string sqlCount = "SELECT COUNT(*) FROM bairros;";
        const string sqlData = @"
            SELECT b.id, b.bairro, ci.id AS CidadeId, ci.id AS Id, ci.cidade, ci.ddd, st.id AS EstadoId, st.id AS Id, st.estado, st.uf, p.id AS PaisId, p.id AS Id, p.pais, p.codigo_iso_pais, p.ddi, p.codigo_iso_moeda, p.simbolo_moeda
            FROM bairros b
            INNER JOIN cidades ci ON ci.id = b.cidade_id
            INNER JOIN estados st ON st.id = ci.estado_id
            INNER JOIN paises p ON p.id = st.pais_id
            ORDER BY b.id DESC
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        var total = await _session.Connection.ExecuteScalarAsync<int>(sqlCount, transaction: _session.Transaction);
        var itens = await _session.Connection.QueryAsync<Bairros, Cidades, Estados, Paises, Bairros>(
            sqlData,
            (bairro, cidade, estado, pais) => { estado.AtualizarResultado(estado.Estado, estado.Uf, pais); cidade.AtualizarResultado(cidade.Cidade, cidade.Ddd, estado); bairro.AtualizarResultado(bairro.Bairro, cidade); return bairro; },
            new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction,
            splitOn: "CidadeId,EstadoId,PaisId"
        );
        return new ResultadoPaginado<Bairros>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<Bairros?> ObterBairroPorId(int id)
    {
        const string sql = @"
            SELECT b.id, b.bairro, ci.id AS CidadeId, ci.id AS Id, ci.cidade, ci.ddd, st.id AS EstadoId, st.id AS Id, st.estado, st.uf, p.id AS PaisId, p.id AS Id, p.pais, p.codigo_iso_pais, p.ddi, p.codigo_iso_moeda, p.simbolo_moeda
            FROM bairros b
            INNER JOIN cidades ci ON ci.id = b.cidade_id
            INNER JOIN estados st ON st.id = ci.estado_id
            INNER JOIN paises p ON p.id = st.pais_id
            WHERE b.id = @Id;";

        var result = await _session.Connection.QueryAsync<Bairros, Cidades, Estados, Paises, Bairros>(
            sql,
            (bairro, cidade, estado, pais) => { estado.AtualizarResultado(estado.Estado, estado.Uf, pais); cidade.AtualizarResultado(cidade.Cidade, cidade.Ddd, estado); bairro.AtualizarResultado(bairro.Bairro, cidade); return bairro; },
            new { Id = id },
            transaction: _session.Transaction,
            splitOn: "CidadeId,EstadoId,PaisId"
        );
        return result.SingleOrDefault();
    }

    public async Task<Bairros> CriarBairro(Bairros bairro)
    {
        const string sql = "INSERT INTO bairros (bairro, cidade_id) VALUES (@Bairro, @CidadeId) RETURNING id;";
        var id = await _session.Connection.ExecuteScalarAsync<int>(sql, new { bairro.Bairro, CidadeId = bairro.Cidade.Id }, transaction: _session.Transaction);
        return new Bairros(id, bairro.Bairro, bairro.Cidade);
    }

    public async Task<Bairros> AtualizarBairro(int id, Bairros bairro)
    {
        const string sql = "UPDATE bairros SET bairro = @Bairro, cidade_id = @CidadeId WHERE id = @Id;";
        await _session.Connection.ExecuteAsync(sql, new { Id = id, bairro.Bairro, CidadeId = bairro.Cidade.Id }, transaction: _session.Transaction);
        return new Bairros(id, bairro.Bairro, bairro.Cidade);
    }

    public async Task<bool> DeletarBairro(int id)
    {
        const string sql = "DELETE FROM bairros WHERE id = @Id;";
        return await _session.Connection.ExecuteAsync(sql, new { Id = id }, transaction: _session.Transaction) > 0;
    }

    public async Task<ResultadoPaginado<Bairros>> PesquisarBairros(string termo, int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;
        const string sqlCount = "SELECT COUNT(*) FROM bairros WHERE bairro ILIKE @Termo;";
        const string sqlData = @"
            SELECT b.id, b.bairro, ci.id AS CidadeId, ci.id AS Id, ci.cidade, ci.ddd, st.id AS EstadoId, st.id AS Id, st.estado, st.uf, p.id AS PaisId, p.id AS Id, p.pais, p.codigo_iso_pais, p.ddi, p.codigo_iso_moeda, p.simbolo_moeda
            FROM bairros b
            INNER JOIN cidades ci ON ci.id = b.cidade_id
            INNER JOIN estados st ON st.id = ci.estado_id
            INNER JOIN paises p ON p.id = st.pais_id
            WHERE b.bairro ILIKE @Termo
            ORDER BY b.id DESC
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        var total = await _session.Connection.ExecuteScalarAsync<int>(sqlCount, new { Termo = $"%{termo}%" }, transaction: _session.Transaction);
        var itens = await _session.Connection.QueryAsync<Bairros, Cidades, Estados, Paises, Bairros>(
            sqlData,
            (bairro, cidade, estado, pais) => { estado.AtualizarResultado(estado.Estado, estado.Uf, pais); cidade.AtualizarResultado(cidade.Cidade, cidade.Ddd, estado); bairro.AtualizarResultado(bairro.Bairro, cidade); return bairro; },
            new { Termo = $"%{termo}%", TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction,
            splitOn: "CidadeId,EstadoId,PaisId"
        );
        return new ResultadoPaginado<Bairros>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<bool> ExisteBairro(string bairro, int cidadeId, int? ignorarId = null)
    {
        var sql = "SELECT COUNT(1) FROM bairros WHERE bairro = @Bairro AND cidade_id = @CidadeId";
        if (ignorarId.HasValue) sql += " AND id != @IgnorarId";
        return await _session.Connection.ExecuteScalarAsync<int>(sql, new { Bairro = bairro, CidadeId = cidadeId, IgnorarId = ignorarId }, transaction: _session.Transaction) > 0;
    }
}
