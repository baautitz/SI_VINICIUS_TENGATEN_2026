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

    public async Task<ResultadoPaginado<Paises>> ObterPaises(int pagina = 1, int tamanhoPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoPagina;

        const string sql = @"
            SELECT COUNT(*) FROM paises;

            SELECT * FROM paises 
            ORDER BY pais 
            LIMIT @TamanhoPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql,
            new { TamanhoPagina = tamanhoPagina, Offset = offset },
            transaction: _session.Transaction
        );

        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<Paises>();

        return new ResultadoPaginado<Paises>(itens, total, pagina, tamanhoPagina);
    }

    public async Task<Paises?> ObterPaisPorId(int id)
    {
        const string sql = "SELECT * FROM paises WHERE id = @Id;";

        return await _session.Connection.QuerySingleOrDefaultAsync<Paises>(
            sql,
            new { Id = id },
            transaction: _session.Transaction
        );
    }

    public async Task<Paises> CriarPais(Paises pais)
    {
        const string sql = @"
            INSERT INTO paises (ddi, sigla_iso, moeda, simbolo_moeda, pais) 
            VALUES (@Ddi, @SiglaIso, @Moeda, @SimboloMoeda, @Pais) 
            RETURNING id;";

        var idGerado = await _session.Connection.ExecuteScalarAsync<int>(
            sql,
            pais,
            transaction: _session.Transaction
        );

        return new Paises(idGerado, pais.Ddi, pais.SiglaIso, pais.Moeda, pais.SimboloMoeda, pais.Pais);
    }

    public async Task<Paises> AtualizarPais(int id, Paises pais)
    {
        const string sql = @"
            UPDATE paises 
            SET ddi = @Ddi, 
                sigla_iso = @SiglaIso, 
                moeda = @Moeda, 
                simbolo_moeda = @SimboloMoeda, 
                pais = @Pais 
            WHERE id = @Id;";

        var parametros = new
        {
            Id = id,
            pais.Ddi,
            pais.SiglaIso,
            pais.Moeda,
            pais.SimboloMoeda,
            pais.Pais
        };

        await _session.Connection.ExecuteAsync(sql, parametros, transaction: _session.Transaction);

        return new Paises(id, pais.Ddi, pais.SiglaIso, pais.Moeda, pais.SimboloMoeda, pais.Pais);
    }

    public async Task<bool> DeletarPais(int id)
    {
        const string sql = "DELETE FROM paises WHERE id = @Id;";

        var linhasAfetadas = await _session.Connection.ExecuteAsync(
            sql,
            new { Id = id },
            transaction: _session.Transaction
        );

        return linhasAfetadas > 0;
    }

    public async Task<ResultadoPaginado<PaisesResumo>> ObterPaisesResumo(int pagina = 1, int tamanhoPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoPagina;

        const string sql = @"
            SELECT COUNT(*) FROM paises;

            SELECT id, pais, sigla_iso, ddi, moeda, simbolo_moeda 
            FROM paises 
            ORDER BY pais 
            LIMIT @TamanhoPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql,
            new { TamanhoPagina = tamanhoPagina, Offset = offset },
            transaction: _session.Transaction
        );

        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<PaisesResumo>();

        return new ResultadoPaginado<PaisesResumo>(itens, total, pagina, tamanhoPagina);
    }

    public async Task<ResultadoPaginado<PaisesResumo>> PesquisarPaises(string termo, int pagina = 1, int tamanhoPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoPagina;

        const string sql = @"
            SELECT COUNT(*) 
            FROM paises 
            WHERE pais ILIKE @Termo OR sigla_iso ILIKE @Termo;

            SELECT id, pais, sigla_iso, ddi, moeda, simbolo_moeda 
            FROM paises 
            WHERE pais ILIKE @Termo OR sigla_iso ILIKE @Termo 
            ORDER BY pais 
            LIMIT @TamanhoPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql,
            new { Termo = $"%{termo}%", TamanhoPagina = tamanhoPagina, Offset = offset },
            transaction: _session.Transaction
        );

        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<PaisesResumo>();

        return new ResultadoPaginado<PaisesResumo>(itens, total, pagina, tamanhoPagina);
    }
}