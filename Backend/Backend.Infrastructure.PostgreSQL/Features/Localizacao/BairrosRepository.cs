using Backend.Core.Common;
using Backend.Core.Features.Localizacao.DTOs;
using Backend.Core.Features.Localizacao.Entities;
using Backend.Core.Features.Localizacao.Repositories;
using Backend.Infrastructure.PostgreSQL.Common;
using Dapper;

namespace Backend.Infrastructure.PostgreSQL.Features.Localizacao;

public class BairrosRepository : IBairrosRepository
{
    private readonly DbSession _session;

    public BairrosRepository(DbSession session)
    {
        _session = session;
    }

    public async Task<ResultadoPaginado<Bairros>> ObterBairros(int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string countSql = "SELECT COUNT(*) FROM bairros;";

        const string querySql = @"
            SELECT b.id AS Id, b.bairro,
                   c.id AS CidadeId, c.cidade, c.ddd,
                   e.id AS EstadoId, e.estado, e.uf,
                   p.id AS PaisId, p.pais, p.sigla_iso, p.ddi, p.moeda, p.simbolo_moeda
            FROM bairros b
            JOIN cidades c ON c.id = b.cidade_id
            JOIN estados e ON e.id = c.estado_id
            JOIN paises p ON p.id = e.pais_id
            ORDER BY b.bairro
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        var total = await _session.Connection.ExecuteScalarAsync<int>(
            countSql, transaction: _session.Transaction);

        var itens = await _session.Connection.QueryAsync<Bairros, Cidades, Estados, Paises, Bairros>(
            querySql,
            (bairro, cidade, estado, pais) =>
            {
                estado.Pais = pais;
                cidade.Estado = estado;
                bairro.Cidade = cidade;
                return bairro;
            },
            new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction,
            splitOn: "CidadeId,EstadoId,PaisId"
        );

        return new ResultadoPaginado<Bairros>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<Bairros?> ObterBairroPorId(int id)
    {
        const string sql = @"
            SELECT b.id AS Id, b.bairro,
                   c.id AS CidadeId, c.cidade, c.ddd,
                   e.id AS EstadoId, e.estado, e.uf,
                   p.id AS PaisId, p.pais, p.sigla_iso, p.ddi, p.moeda, p.simbolo_moeda
            FROM bairros b
            JOIN cidades c ON c.id = b.cidade_id
            JOIN estados e ON e.id = c.estado_id
            JOIN paises p ON p.id = e.pais_id
            WHERE b.id = @Id;";

        var result = await _session.Connection.QueryAsync<Bairros, Cidades, Estados, Paises, Bairros>(
            sql,
            (bairro, cidade, estado, pais) =>
            {
                estado.Pais = pais;
                cidade.Estado = estado;
                bairro.Cidade = cidade;
                return bairro;
            },
            new { Id = id },
            transaction: _session.Transaction,
            splitOn: "CidadeId,EstadoId,PaisId"
        );

        return result.SingleOrDefault();
    }

    public async Task<Bairros> CriarBairro(Bairros bairro)
    {
        const string sql = @"
            INSERT INTO bairros (bairro, cidade_id)
            VALUES (@Bairro, @CidadeId)
            RETURNING id;";

        var idGerado = await _session.Connection.ExecuteScalarAsync<int>(
            sql,
            new { bairro.Bairro, CidadeId = bairro.Cidade.Id },
            transaction: _session.Transaction
        );

        bairro.Id = idGerado;
        return bairro;
    }

    public async Task<Bairros> AtualizarBairro(int id, Bairros bairro)
    {
        const string sql = @"
            UPDATE bairros
            SET bairro = @Bairro,
                cidade_id = @CidadeId
            WHERE id = @Id;";

        await _session.Connection.ExecuteAsync(
            sql,
            new { Id = id, bairro.Bairro, CidadeId = bairro.Cidade.Id },
            transaction: _session.Transaction
        );

        bairro.Id = id;
        return bairro;
    }

    public async Task<bool> DeletarBairro(int id)
    {
        const string sql = "DELETE FROM bairros WHERE id = @Id;";

        var linhasAfetadas = await _session.Connection.ExecuteAsync(
            sql,
            new { Id = id },
            transaction: _session.Transaction
        );

        return linhasAfetadas > 0;
    }

    public async Task<ResultadoPaginado<BairrosResumo>> ObterBairrosResumo(int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sql = @"
            SELECT COUNT(*) FROM bairros;

            SELECT id, bairro
            FROM bairros
            ORDER BY bairro
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql,
            new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction
        );

        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<BairrosResumo>();

        return new ResultadoPaginado<BairrosResumo>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<ResultadoPaginado<BairrosResumo>> PesquisarBairros(string termo, int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sql = @"
            SELECT COUNT(*)
            FROM bairros
            WHERE bairro ILIKE @Termo;

            SELECT id, bairro
            FROM bairros
            WHERE bairro ILIKE @Termo
            ORDER BY bairro
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql,
            new { Termo = $"%{termo}%", TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction
        );

        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<BairrosResumo>();

        return new ResultadoPaginado<BairrosResumo>(itens, total, pagina, tamanhoDaPagina);
    }
}
