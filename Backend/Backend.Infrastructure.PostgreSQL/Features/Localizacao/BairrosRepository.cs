using Backend.Core.Common.Results;
using Backend.Core.Features.Localizacao.DTOs;
using Backend.Core.Features.Localizacao.Entities;
using Backend.Core.Features.Localizacao.Repositories;
using Backend.Infrastructure.PostgreSQL.Common;
using Dapper;
using Npgsql;

namespace Backend.Infrastructure.PostgreSQL.Features.Localizacao;

public class BairrosRepository : IBairrosRepository
{
    private readonly DbSession _session;

    public BairrosRepository(DbSession session)
    {
        _session = session;
    }

    public async Task<ResultadoPaginado<BairroResumoDto>> ObterBairros(string? search, int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;
        
        string sql;
        object parametros;

        if (string.IsNullOrWhiteSpace(search))
        {
            sql = @"
                SELECT COUNT(*) FROM bairros;
                SELECT b.id, b.bairro AS Bairro, c.id AS CidadeId, c.cidade AS CidadeNome, e.id AS EstadoId, e.uf AS Uf
                FROM bairros b
                JOIN cidades c ON c.id = b.cidade_id
                JOIN estados e ON e.id = c.estado_id
                ORDER BY b.bairro LIMIT @TamanhoDaPagina OFFSET @Offset;";
            parametros = new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset };
        }
        else
        {
            sql = @"
                SELECT COUNT(*) FROM bairros WHERE unaccent(bairro::text) ILIKE unaccent(@Termo::text);
                SELECT b.id, b.bairro AS Bairro, c.id AS CidadeId, c.cidade AS CidadeNome, e.id AS EstadoId, e.uf AS Uf
                FROM bairros b
                JOIN cidades c ON c.id = b.cidade_id
                JOIN estados e ON e.id = c.estado_id
                WHERE unaccent(b.bairro::text) ILIKE unaccent(@Termo::text)
                ORDER BY b.bairro LIMIT @TamanhoDaPagina OFFSET @Offset;";
            parametros = new { Termo = "%" + search + "%", TamanhoDaPagina = tamanhoDaPagina, Offset = offset };
        }

        using var multi = await _session.Connection.QueryMultipleAsync(sql, parametros, transaction: _session.Transaction);
        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<BairroResumoDto>();
        return new ResultadoPaginado<BairroResumoDto>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<Bairros?> ObterBairroPorId(int id)
    {
        const string sql = @"
            SELECT b.id, b.bairro,
                   c.id AS cidade_id, c.id, c.cidade, c.ddd,
                   e.id AS estado_id, e.id, e.estado, e.uf,
                   p.id AS pais_id, p.id, p.pais, p.sigla_iso, p.ddi, p.moeda, p.simbolo_moeda
            FROM bairros b
            JOIN cidades c ON c.id = b.cidade_id
            JOIN estados e ON e.id = c.estado_id
            JOIN paises p ON p.id = e.pais_id
            WHERE b.id = @Id;";
        var result = await _session.Connection.QueryAsync<Bairros, Cidades, Estados, Paises, Bairros>(
            sql,
            (bairro, cidade, estado, pais) => {
                estado.AtualizarResultado(estado.Estado, estado.Uf, pais);
                cidade.AtualizarResultado(cidade.Cidade, cidade.Ddd, estado);
                bairro.AtualizarResultado(bairro.Bairro, cidade);
                return bairro;
            },
            new { Id = id },
            transaction: _session.Transaction,
            splitOn: "cidade_id,estado_id,pais_id"
        );
        return result.SingleOrDefault();
    }

    public async Task<Bairros> CriarBairro(Bairros bairro)
    {
        try
        {
            const string sql = "INSERT INTO bairros (bairro, cidade_id) VALUES (@Bairro, @CidadeId) RETURNING id;";
            var idGerado = await _session.Connection.ExecuteScalarAsync<int>(sql, new { bairro.Bairro, CidadeId = bairro.Cidade.Id }, transaction: _session.Transaction);
            return new Bairros(idGerado, bairro.Bairro, bairro.Cidade);
        }
        catch (PostgresException ex)
        {
            throw DbExceptionTranslator.Translate(ex);
        }
    }

    public async Task<Bairros> AtualizarBairro(int id, Bairros bairro)
    {
        try
        {
            const string sql = "UPDATE bairros SET bairro = @Bairro, cidade_id = @CidadeId WHERE id = @Id;";
            await _session.Connection.ExecuteAsync(sql, new { Id = id, bairro.Bairro, CidadeId = bairro.Cidade.Id }, transaction: _session.Transaction);
            return new Bairros(id, bairro.Bairro, bairro.Cidade);
        }
        catch (PostgresException ex)
        {
            throw DbExceptionTranslator.Translate(ex);
        }
    }

    public async Task<bool> ExisteBairro(int cidadeId, string bairro, int? ignorarId = null)
    {
        var sql = "SELECT COUNT(1) FROM bairros WHERE cidade_id = @CidadeId AND bairro = @Bairro";
        if (ignorarId.HasValue) sql += " AND id != @IgnorarId";
        return await _session.Connection.ExecuteScalarAsync<int>(sql, new { CidadeId = cidadeId, Bairro = bairro, IgnorarId = ignorarId }, transaction: _session.Transaction) > 0;
    }

    public async Task<bool> DeletarBairro(int id)
    {
        try
        {
            const string sql = "DELETE FROM bairros WHERE id = @Id;";
            var rows = await _session.Connection.ExecuteAsync(sql, new { Id = id }, transaction: _session.Transaction);
            return rows > 0;
        }
        catch (PostgresException ex)
        {
            throw DbExceptionTranslator.Translate(ex);
        }
    }
}