using Backend.Core.Common;
using Backend.Core.Features.Localizacao.Entities;
using Backend.Core.Features.Logistica.DTOs;
using Backend.Core.Features.Logistica.Entities;
using Backend.Core.Features.Logistica.Repositories;
using Backend.Infrastructure.PostgreSQL.Common;
using Dapper;

namespace Backend.Infrastructure.PostgreSQL.Features.Logistica;

public class TransportadorasRepository : ITransportadorasRepository
{
    private readonly DbSession _session;

    public TransportadorasRepository(DbSession session)
    {
        _session = session;
    }

    public async Task<ResultadoPaginado<Transportadoras>> ObterTransportadoras(int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sql = @"
            SELECT COUNT(*) FROM transportadoras;

            SELECT id AS Id, nome_razaosocial AS NomeRazaosocial, cpf_cnpj AS CpfCnpj, rg_ie AS RgIe, apelido_nomefantasia AS ApelidoNomefantasia,
                   endereco AS Endereco, telefone AS Telefone, email AS Email, rntrc AS Rntrc, ativo AS Ativo, criado_em AS CriadoEm,
                   observacao AS Observacao
            FROM transportadoras
            ORDER BY nome_razaosocial
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql,
            new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction
        );

        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<Transportadoras>();

        return new ResultadoPaginado<Transportadoras>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<Transportadoras?> ObterTransportadoraPorId(int id)
    {
        const string sql = @"
            SELECT t.id AS Id, t.nome_razaosocial AS NomeRazaosocial, t.cpf_cnpj AS CpfCnpj, t.rg_ie AS RgIe, t.apelido_nomefantasia AS ApelidoNomefantasia,
                   t.endereco AS Endereco, t.telefone AS Telefone, t.email AS Email, t.rntrc AS Rntrc, t.ativo AS Ativo, t.criado_em AS CriadoEm,
                   t.observacao AS Observacao,
                   b.id AS Id, b.bairro,
                   ci.id AS Id, ci.cidade, ci.ddd,
                   e.id AS Id, e.estado, e.uf,
                   p.id AS Id, p.pais, p.sigla_iso, p.ddi, p.moeda, p.simbolo_moeda
            FROM transportadoras t
            LEFT JOIN bairros b ON b.id = t.bairro_id
            LEFT JOIN cidades ci ON ci.id = b.cidade_id
            LEFT JOIN estados e ON e.id = ci.estado_id
            LEFT JOIN paises p ON p.id = e.pais_id
            WHERE t.id = @Id;";

        var result = await _session.Connection.QueryAsync<Transportadoras, Bairros, Cidades, Estados, Paises, Transportadoras>(
            sql,
            (transportadora, bairro, cidade, estado, pais) =>
            {
                if (bairro is not null)
                {
                    if (pais is not null && estado is not null) estado.AtualizarResultado(estado.Estado, estado.Uf, pais);
                    if (estado is not null && cidade is not null) cidade.AtualizarResultado(cidade.Cidade, cidade.Ddd, estado);
                    if (cidade is not null) bairro.AtualizarResultado(bairro.Bairro, cidade);
                    transportadora.Atualizar(transportadora.NomeRazaosocial, transportadora.CpfCnpj, transportadora.RgIe, transportadora.ApelidoNomefantasia, transportadora.Endereco, bairro, transportadora.Telefone, transportadora.Email, transportadora.Rntrc, transportadora.Observacao);
                }
                return transportadora;
            },
            new { Id = id },
            transaction: _session.Transaction,
            splitOn: "Id,Id,Id,Id"
        );

        return result.SingleOrDefault();
    }

    public async Task<Transportadoras> CriarTransportadora(Transportadoras transportadora)
    {
        const string sql = @"
            INSERT INTO transportadoras (nome_razaosocial, cpf_cnpj, rg_ie, apelido_nomefantasia,
                                        endereco, bairro_id, telefone, email, rntrc,
                                        ativo, criado_em, observacao)
            VALUES (@NomeRazaosocial, @CpfCnpj, @RgIe, @ApelidoNomefantasia,
                    @Endereco, @BairroId, @Telefone, @Email, @Rntrc,
                    @Ativo, @CriadoEm, @Observacao)
            RETURNING id;";

        var idGerado = await _session.Connection.ExecuteScalarAsync<int>(
            sql,
            new
            {
                transportadora.NomeRazaosocial,
                transportadora.CpfCnpj,
                transportadora.RgIe,
                transportadora.ApelidoNomefantasia,
                transportadora.Endereco,
                BairroId = transportadora.Bairro?.Id,
                transportadora.Telefone,
                transportadora.Email,
                transportadora.Rntrc,
                transportadora.Ativo,
                CriadoEm = DateTime.UtcNow,
                transportadora.Observacao
            },
            transaction: _session.Transaction
        );

        transportadora.Id = idGerado;
        return transportadora;
    }

    public async Task<Transportadoras> AtualizarTransportadora(int id, Transportadoras transportadora)
    {
        const string sql = @"
            UPDATE transportadoras
            SET nome_razaosocial = @NomeRazaosocial, cpf_cnpj = @CpfCnpj,
                rg_ie = @RgIe, apelido_nomefantasia = @ApelidoNomefantasia,
                endereco = @Endereco, bairro_id = @BairroId, telefone = @Telefone,
                email = @Email, rntrc = @Rntrc, ativo = @Ativo,
                atualizado_em = @AtualizadoEm, observacao = @Observacao
            WHERE id = @Id;";

        await _session.Connection.ExecuteAsync(
            sql,
            new
            {
                Id = id,
                transportadora.NomeRazaosocial,
                transportadora.CpfCnpj,
                transportadora.RgIe,
                transportadora.ApelidoNomefantasia,
                transportadora.Endereco,
                BairroId = transportadora.Bairro?.Id,
                transportadora.Telefone,
                transportadora.Email,
                transportadora.Rntrc,
                transportadora.Ativo,
                AtualizadoEm = DateTime.UtcNow,
                transportadora.Observacao
            },
            transaction: _session.Transaction
        );

        transportadora.Id = id;
        return transportadora;
    }

    public async Task<bool> DeletarTransportadora(int id)
    {
        const string sql = "DELETE FROM transportadoras WHERE id = @Id;";

        var linhasAfetadas = await _session.Connection.ExecuteAsync(
            sql, new { Id = id }, transaction: _session.Transaction);

        return linhasAfetadas > 0;
    }

    public async Task<ResultadoPaginado<TransportadorasResumo>> ObterTransportadorasResumo(int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sql = @"
            SELECT COUNT(*) FROM transportadoras;

            SELECT id, nome_razaosocial AS NomeRazaosocial, cpf_cnpj AS CpfCnpj, ativo AS Ativo
            FROM transportadoras ORDER BY nome_razaosocial
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql, new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction);

        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<TransportadorasResumo>();

        return new ResultadoPaginado<TransportadorasResumo>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<ResultadoPaginado<TransportadorasResumo>> PesquisarTransportadoras(string termo, int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sql = @"
            SELECT COUNT(*) FROM transportadoras
            WHERE nome_razaosocial ILIKE @Termo OR cpf_cnpj ILIKE @Termo
               OR apelido_nomefantasia ILIKE @Termo OR email ILIKE @Termo;

            SELECT id, nome_razaosocial AS NomeRazaosocial, cpf_cnpj AS CpfCnpj, ativo AS Ativo
            FROM transportadoras
            WHERE nome_razaosocial ILIKE @Termo OR cpf_cnpj ILIKE @Termo
               OR apelido_nomefantasia ILIKE @Termo OR email ILIKE @Termo
            ORDER BY nome_razaosocial
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql,
            new { Termo = $"%{termo}%", TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction);

        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<TransportadorasResumo>();

        return new ResultadoPaginado<TransportadorasResumo>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<bool> ExisteTransportadoraCpfCnpj(string cpfCnpj, int? ignorarId = null)
    {
        var sql = @"
            SELECT COUNT(1)
            FROM transportadoras
            WHERE cpf_cnpj = @CpfCnpj";

        if (ignorarId.HasValue)
        {
            sql += " AND id != @IgnorarId";
        }

        var count = await _session.Connection.ExecuteScalarAsync<int>(
            sql,
            new { CpfCnpj = cpfCnpj, IgnorarId = ignorarId },
            transaction: _session.Transaction);

        return count > 0;
    }
}
