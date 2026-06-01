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

    public async Task<ResultadoPaginado<BairroResumoDto>> ObterBairros(string? search, int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;
        
        string sql;
        object parametros;

        if (string.IsNullOrWhiteSpace(search))
        {
            sql = @"
                SELECT COUNT(*) FROM bairros b JOIN cidades c ON c.id = b.cidade_id JOIN estados e ON e.id = c.estado_id;
                SELECT b.id, b.bairro AS Bairro, c.id AS CidadeId, c.cidade AS CidadeNome, e.uf AS Uf
                FROM bairros b
                JOIN cidades c ON c.id = b.cidade_id
                JOIN estados e ON e.id = c.estado_id
                ORDER BY b.bairro LIMIT @TamanhoDaPagina OFFSET @Offset;";
            parametros = new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset };
        }
        else
        {
            sql = @"
                SELECT COUNT(*) FROM bairros b JOIN cidades c ON c.id = b.cidade_id JOIN estados e ON e.id = c.estado_id WHERE unaccent(b.bairro::text) ILIKE unaccent(@Termo::text);
                SELECT b.id, b.bairro AS Bairro, c.id AS CidadeId, c.cidade AS CidadeNome, e.uf AS Uf
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
        const string sql = "INSERT INTO bairros (bairro, cidade_id) VALUES (@Bairro, @CidadeId) RETURNING id;";
        var idGerado = await _session.Connection.ExecuteScalarAsync<int>(sql, new { bairro.Bairro, CidadeId = bairro.Cidade.Id }, transaction: _session.Transaction);
        return new Bairros(idGerado, bairro.Bairro, bairro.Cidade);
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
        var rows = await _session.Connection.ExecuteAsync(sql, new { Id = id }, transaction: _session.Transaction);
        return rows > 0;
    }
}