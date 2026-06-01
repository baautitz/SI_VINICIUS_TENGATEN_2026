using Backend.Core.Common;
using Backend.Core.Features.Localizacao.DTOs;
using Backend.Core.Features.Localizacao.Entities;
using Backend.Core.Features.Localizacao.Repositories;
using Backend.Infrastructure.PostgreSQL.Common;
using Dapper;

namespace Backend.Infrastructure.PostgreSQL.Features.Localizacao;

public class PaisesRepository : IPaisesRepository
{
    private readonly DbSession _session;

    public PaisesRepository(DbSession session)
    {
        _session = session;
    }

    public async Task<ResultadoPaginado<PaisResumoDto>> ObterPaises(string? search, int pagina = 1, int tamanhoPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoPagina;

        string sql;
        object parametros;

        if (string.IsNullOrWhiteSpace(search))
        {
            sql = @"
                SELECT COUNT(*) FROM paises;
                SELECT id, pais AS Pais, sigla_iso AS SiglaIso, ddi AS Ddi, moeda AS Moeda, simbolo_moeda AS SimboloMoeda
                FROM paises ORDER BY pais LIMIT @TamanhoPagina OFFSET @Offset;";
            parametros = new { TamanhoPagina = tamanhoPagina, Offset = offset };
        }
        else
        {
            sql = @"
                SELECT COUNT(*) FROM paises WHERE unaccent(pais::text) ILIKE unaccent(@Termo::text) OR unaccent(sigla_iso::text) ILIKE unaccent(@Termo::text);
                SELECT id, pais AS Pais, sigla_iso AS SiglaIso, ddi AS Ddi, moeda AS Moeda, simbolo_moeda AS SimboloMoeda
                FROM paises WHERE unaccent(pais::text) ILIKE unaccent(@Termo::text) OR unaccent(sigla_iso::text) ILIKE unaccent(@Termo::text)
                ORDER BY pais LIMIT @TamanhoPagina OFFSET @Offset;";
            parametros = new { Termo = "%" + search + "%", TamanhoPagina = tamanhoPagina, Offset = offset };
        }

        using var multi = await _session.Connection.QueryMultipleAsync(sql, parametros, transaction: _session.Transaction);
        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<PaisResumoDto>();
        return new ResultadoPaginado<PaisResumoDto>(itens, total, pagina, tamanhoPagina);
    }

    public async Task<Paises?> ObterPaisPorId(int id)
    {
        const string sql = "SELECT id, pais, sigla_iso AS SiglaIso, ddi, moeda, simbolo_moeda AS SimboloMoeda FROM paises WHERE id = @Id;";
        return await _session.Connection.QuerySingleOrDefaultAsync<Paises>(sql, new { Id = id }, transaction: _session.Transaction);
    }

    public Task<Paises> CriarPais(Paises pais)
    {
        return DbSessionExtensions.ExecuteWithConflictCheckAsync(async () =>
        {
            const string sql = "INSERT INTO paises (ddi, sigla_iso, moeda, simbolo_moeda, pais) VALUES (@Ddi, @SiglaIso, @Moeda, @SimboloMoeda, @Pais) RETURNING id;";
            var idGerado = await _session.Connection.ExecuteScalarAsync<int>(sql, new { pais.Ddi, pais.SiglaIso, pais.Moeda, pais.SimboloMoeda, pais.Pais }, transaction: _session.Transaction);
            return new Paises(idGerado, pais.Ddi, pais.SiglaIso, pais.Moeda, pais.SimboloMoeda, pais.Pais);
        });
    }

    public Task<Paises> AtualizarPais(int id, Paises pais)
    {
        return DbSessionExtensions.ExecuteWithConflictCheckAsync(async () =>
        {
            const string sql = "UPDATE paises SET ddi = @Ddi, sigla_iso = @SiglaIso, moeda = @Moeda, simbolo_moeda = @SimboloMoeda, pais = @Pais WHERE id = @Id;";
            await _session.Connection.ExecuteAsync(sql, new { Id = id, pais.Ddi, pais.SiglaIso, pais.Moeda, pais.SimboloMoeda, pais.Pais }, transaction: _session.Transaction);
            return new Paises(id, pais.Ddi, pais.SiglaIso, pais.Moeda, pais.SimboloMoeda, pais.Pais);
        });
    }

    public async Task<bool> ExistePais(string siglaIso, string pais, int? ignorarId = null)
    {
        var sql = "SELECT COUNT(1) FROM paises WHERE (sigla_iso = @SiglaIso OR pais = @Pais)";
        if (ignorarId.HasValue) sql += " AND id != @IgnorarId";
        return await _session.Connection.ExecuteScalarAsync<int>(sql, new { SiglaIso = siglaIso, Pais = pais, IgnorarId = ignorarId }, transaction: _session.Transaction) > 0;
    }

    public async Task<bool> DeletarPais(int id)
    {
        const string sql = "DELETE FROM paises WHERE id = @Id;";
        var rows = await _session.Connection.ExecuteAsync(sql, new { Id = id }, transaction: _session.Transaction);
        return rows > 0;
    }
}