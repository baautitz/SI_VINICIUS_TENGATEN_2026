using System.Linq;
using Backend.Core.Common.Results;
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

    public async Task<ResultadoPaginado<Paises>> ObterPaises(int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;
        const string sqlCount = "SELECT COUNT(*) FROM paises;";
        const string sqlData = @"
            SELECT id, pais, codigo_iso_pais AS CodigoIsoPais, ddi, codigo_iso_moeda AS CodigoIsoMoeda, simbolo_moeda AS SimboloMoeda
            FROM paises
            ORDER BY pais
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        var total = await _session.Connection.ExecuteScalarAsync<int>(sqlCount, transaction: _session.Transaction);
        var itens = await _session.Connection.QueryAsync<Paises>(
            sqlData,
            new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction
        );
        return new ResultadoPaginado<Paises>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<Paises?> ObterPaisPorId(int id)
    {
        const string sql = "SELECT id, pais, codigo_iso_pais AS CodigoIsoPais, ddi, codigo_iso_moeda AS CodigoIsoMoeda, simbolo_moeda AS SimboloMoeda FROM paises WHERE id = @Id;";
        return await _session.Connection.QuerySingleOrDefaultAsync<Paises>(sql, new { Id = id }, transaction: _session.Transaction);
    }

    public async Task<Paises> CriarPais(Paises pais)
    {
        const string sql = "INSERT INTO paises (ddi, codigo_iso_pais, codigo_iso_moeda, simbolo_moeda, pais) VALUES (@Ddi, @CodigoIsoPais, @CodigoIsoMoeda, @SimboloMoeda, @Pais) RETURNING id;";
        var idGerado = await _session.Connection.ExecuteScalarAsync<int>(sql, new { pais.Ddi, pais.CodigoIsoPais, pais.CodigoIsoMoeda, pais.SimboloMoeda, pais.Pais }, transaction: _session.Transaction);
        return new Paises(idGerado, pais.Ddi, pais.CodigoIsoPais, pais.CodigoIsoMoeda, pais.SimboloMoeda, pais.Pais);
    }

    public async Task<Paises> AtualizarPais(int id, Paises pais)
    {
        const string sql = "UPDATE paises SET ddi = @Ddi, codigo_iso_pais = @CodigoIsoPais, codigo_iso_moeda = @CodigoIsoMoeda, simbolo_moeda = @SimboloMoeda, pais = @Pais WHERE id = @Id;";
        await _session.Connection.ExecuteAsync(sql, new { Id = id, pais.Ddi, pais.CodigoIsoPais, pais.CodigoIsoMoeda, pais.SimboloMoeda, pais.Pais }, transaction: _session.Transaction);
        return new Paises(id, pais.Ddi, pais.CodigoIsoPais, pais.CodigoIsoMoeda, pais.SimboloMoeda, pais.Pais);
    }

    public async Task<ResultadoPaginado<Paises>> PesquisarPaises(string termo, int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;
        const string sqlCount = "SELECT COUNT(*) FROM paises WHERE pais ILIKE @Termo OR codigo_iso_pais ILIKE @Termo;";
        const string sqlData = @"
            SELECT id, pais, codigo_iso_pais AS CodigoIsoPais, ddi, codigo_iso_moeda AS CodigoIsoMoeda, simbolo_moeda AS SimboloMoeda
            FROM paises
            WHERE pais ILIKE @Termo OR codigo_iso_pais ILIKE @Termo
            ORDER BY pais
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        var total = await _session.Connection.ExecuteScalarAsync<int>(sqlCount, new { Termo = $"%{termo}%" }, transaction: _session.Transaction);
        var itens = await _session.Connection.QueryAsync<Paises>(
            sqlData,
            new { Termo = $"%{termo}%", TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction
        );
        return new ResultadoPaginado<Paises>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<bool> ExistePais(string codigoIsoPais, string pais, int? ignorarId = null)
    {
        var sql = "SELECT COUNT(1) FROM paises WHERE (codigo_iso_pais = @CodigoIsoPais OR pais = @Pais)";
        if (ignorarId.HasValue) sql += " AND id != @IgnorarId";
        return await _session.Connection.ExecuteScalarAsync<int>(sql, new { CodigoIsoPais = codigoIsoPais, Pais = pais, IgnorarId = ignorarId }, transaction: _session.Transaction) > 0;
    }

    public async Task<bool> DeletarPais(int id)
    {
        const string sql = "DELETE FROM paises WHERE id = @Id;";
        var rows = await _session.Connection.ExecuteAsync(sql, new { Id = id }, transaction: _session.Transaction);
        return rows > 0;
    }
}
