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

    public async Task<ResultadoPaginado<CidadeResumoDto>> ObterCidades(string? search, int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;
        
        string sql;
        object parametros;

        if (string.IsNullOrWhiteSpace(search))
        {
            sql = @"
                SELECT COUNT(*) FROM cidades;
                SELECT c.id, c.cidade AS Cidade, c.ddd AS Ddd, e.id AS EstadoId, e.estado AS EstadoNome, e.uf AS Uf
                FROM cidades c
                JOIN estados e ON e.id = c.estado_id
                ORDER BY c.cidade LIMIT @TamanhoDaPagina OFFSET @Offset;";
            parametros = new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset };
        }
        else
        {
            sql = @"
                SELECT COUNT(*) FROM cidades WHERE unaccent(cidade::text) ILIKE unaccent(@Termo::text);
                SELECT c.id, c.cidade AS Cidade, c.ddd AS Ddd, e.id AS EstadoId, e.estado AS EstadoNome, e.uf AS Uf
                FROM cidades c
                JOIN estados e ON e.id = c.estado_id
                WHERE unaccent(c.cidade::text) ILIKE unaccent(@Termo::text)
                ORDER BY c.cidade LIMIT @TamanhoDaPagina OFFSET @Offset;";
            parametros = new { Termo = "%" + search + "%", TamanhoDaPagina = tamanhoDaPagina, Offset = offset };
        }

        using var multi = await _session.Connection.QueryMultipleAsync(sql, parametros, transaction: _session.Transaction);
        
        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<CidadeResumoDto>();
        
        return new ResultadoPaginado<CidadeResumoDto>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<Cidades?> ObterCidadePorId(int id)
    {
        const string sql = @"
            SELECT c.id, c.cidade, c.ddd,
                   e.id AS estado_id, e.id, e.estado, e.uf,
                   p.id AS pais_id, p.id, p.pais, p.sigla_iso, p.ddi, p.moeda, p.simbolo_moeda
            FROM cidades c
            JOIN estados e ON e.id = c.estado_id
            JOIN paises p ON p.id = e.pais_id
            WHERE c.id = @Id;";
        var result = await _session.Connection.QueryAsync<Cidades, Estados, Paises, Cidades>(
            sql,
            (cidade, estado, pais) => {
                estado.AtualizarResultado(estado.Estado, estado.Uf, pais);
                cidade.AtualizarResultado(cidade.Cidade, cidade.Ddd, estado);
                return cidade;
            },
            new { Id = id },
            transaction: _session.Transaction,
            splitOn: "estado_id,pais_id"
        );
        return result.SingleOrDefault();
    }

    public async Task<Cidades> CriarCidade(Cidades cidade)
    {
        const string sql = "INSERT INTO cidades (cidade, ddd, estado_id) VALUES (@Cidade, @Ddd, @EstadoId) RETURNING id;";
        var idGerado = await _session.Connection.ExecuteScalarAsync<int>(sql, new { cidade.Cidade, cidade.Ddd, EstadoId = cidade.Estado.Id }, transaction: _session.Transaction);
        return new Cidades(idGerado, cidade.Cidade, cidade.Ddd, cidade.Estado);
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
        var rows = await _session.Connection.ExecuteAsync(sql, new { Id = id }, transaction: _session.Transaction);
        return rows > 0;
    }
}