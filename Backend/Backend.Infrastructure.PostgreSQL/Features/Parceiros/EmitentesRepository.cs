using Backend.Core.Common;
using Backend.Core.Features.Localizacao.Entities;
using Backend.Core.Features.Parceiros.DTOs;
using Backend.Core.Features.Parceiros.Entities;
using Backend.Core.Features.Parceiros.Repositories;
using Backend.Infrastructure.PostgreSQL.Common;
using Dapper;

namespace Backend.Infrastructure.PostgreSQL.Features.Parceiros;

public class EmitentesRepository : IEmitentesRepository
{
    private readonly DbSession _session;

    public EmitentesRepository(DbSession session)
    {
        _session = session;
    }

    public async Task<ResultadoPaginado<Emitentes>> ObterEmitentes(int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sql = @"
            SELECT COUNT(*) FROM emitentes;

            SELECT id, nome_razao_social, cpf_cnpj, apelido_nome_fantasia, endereco,
                   telefone, email, rg_ie, inscricao_municipal, regime_tributario,
                   ativo, criado_em, observacao
            FROM emitentes
            ORDER BY nome_razao_social
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql, new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction);

        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<Emitentes>();

        return new ResultadoPaginado<Emitentes>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<Emitentes?> ObterEmitentePorId(int id)
    {
        const string sql = @"
            SELECT e.id AS Id, e.nome_razao_social, e.cpf_cnpj, e.apelido_nome_fantasia,
                   e.endereco, e.telefone, e.email, e.rg_ie, e.inscricao_municipal,
                   e.regime_tributario, e.ativo, e.criado_em, e.observacao,
                   b.id AS BairroId, b.bairro,
                   c.id AS CidadeId, c.cidade, c.ddd,
                   st.id AS EstadoId, st.estado, st.uf,
                   p.id AS PaisId, p.pais, p.sigla_iso, p.ddi, p.moeda, p.simbolo_moeda
            FROM emitentes e
            LEFT JOIN bairros b ON b.id = e.bairro_id
            LEFT JOIN cidades c ON c.id = b.cidade_id
            LEFT JOIN estados st ON st.id = c.estado_id
            LEFT JOIN paises p ON p.id = st.pais_id
            WHERE e.id = @Id;";

        var result = await _session.Connection.QueryAsync<Emitentes, Bairros, Cidades, Estados, Paises, Emitentes>(
            sql,
            (emitente, bairro, cidade, estado, pais) =>
            {
                if (bairro is not null)
                {
                    if (pais is not null && estado is not null) estado.Atualizar(estado.Estado, estado.Uf, pais);
                    if (estado is not null && cidade is not null) cidade.Atualizar(cidade.Cidade, cidade.Ddd, estado);
                    if (cidade is not null) bairro.Atualizar(bairro.Bairro, cidade);
                    emitente.AtualizarDados(emitente.NomeRazaoSocial, emitente.CpfCnpj, emitente.ApelidoNomeFantasia, emitente.Endereco, bairro, emitente.Telefone, emitente.Email, emitente.RgIe, emitente.InscricaoMunicipal, emitente.RegimeTributario, emitente.Observacao);
                }
                return emitente;
            },
            new { Id = id },
            transaction: _session.Transaction,
            splitOn: "BairroId,CidadeId,EstadoId,PaisId"
        );

        return result.SingleOrDefault();
    }

    public async Task<Emitentes> CriarEmitente(Emitentes emitente)
    {
        const string sql = @"
            INSERT INTO emitentes (nome_razao_social, cpf_cnpj, apelido_nome_fantasia, endereco,
                                   bairro_id, telefone, email, rg_ie, inscricao_municipal,
                                   regime_tributario, ativo, criado_em, observacao)
            VALUES (@NomeRazaoSocial, @CpfCnpj, @ApelidoNomeFantasia, @Endereco,
                    @BairroId, @Telefone, @Email, @RgIe, @InscricaoMunicipal,
                    @RegimeTributario, @Ativo, @CriadoEm, @Observacao)
            RETURNING id;";

        var idGerado = await _session.Connection.ExecuteScalarAsync<int>(
            sql,
            new
            {
                emitente.NomeRazaoSocial,
                emitente.CpfCnpj,
                emitente.ApelidoNomeFantasia,
                emitente.Endereco,
                BairroId = emitente.Bairro?.Id,
                emitente.Telefone,
                emitente.Email,
                emitente.RgIe,
                emitente.InscricaoMunicipal,
                emitente.RegimeTributario,
                emitente.Ativo,
                CriadoEm = DateTime.UtcNow,
                emitente.Observacao
            },
            transaction: _session.Transaction
        );

        emitente.Id = idGerado;
        return emitente;
    }

    public async Task<Emitentes> AtualizarEmitente(int id, Emitentes emitente)
    {
        const string sql = @"
            UPDATE emitentes
            SET nome_razao_social = @NomeRazaoSocial, cpf_cnpj = @CpfCnpj,
                apelido_nome_fantasia = @ApelidoNomeFantasia, endereco = @Endereco,
                bairro_id = @BairroId, telefone = @Telefone, email = @Email,
                rg_ie = @RgIe, inscricao_municipal = @InscricaoMunicipal,
                regime_tributario = @RegimeTributario, ativo = @Ativo,
                atualizado_em = @AtualizadoEm, observacao = @Observacao
            WHERE id = @Id;";

        await _session.Connection.ExecuteAsync(
            sql,
            new
            {
                Id = id,
                emitente.NomeRazaoSocial,
                emitente.CpfCnpj,
                emitente.ApelidoNomeFantasia,
                emitente.Endereco,
                BairroId = emitente.Bairro?.Id,
                emitente.Telefone,
                emitente.Email,
                emitente.RgIe,
                emitente.InscricaoMunicipal,
                emitente.RegimeTributario,
                emitente.Ativo,
                emitente.Observacao
            },
            transaction: _session.Transaction
        );

        emitente.Id = id;
        return emitente;
    }

    public async Task<bool> DeletarEmitente(int id)
    {
        const string sql = "DELETE FROM emitentes WHERE id = @Id;";

        var linhasAfetadas = await _session.Connection.ExecuteAsync(
            sql, new { Id = id }, transaction: _session.Transaction);

        return linhasAfetadas > 0;
    }

    public async Task<ResultadoPaginado<EmitentesResumo>> ObterEmitentesResumo(int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sql = @"
            SELECT COUNT(*) FROM emitentes;

            SELECT id, nome_razao_social, cpf_cnpj, apelido_nome_fantasia
            FROM emitentes ORDER BY nome_razao_social
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql, new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction);

        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<EmitentesResumo>();

        return new ResultadoPaginado<EmitentesResumo>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<ResultadoPaginado<EmitentesResumo>> PesquisarEmitentes(string termo, int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sql = @"
            SELECT COUNT(*) FROM emitentes
            WHERE nome_razao_social ILIKE @Termo OR cpf_cnpj ILIKE @Termo
               OR apelido_nome_fantasia ILIKE @Termo OR email ILIKE @Termo;

            SELECT id, nome_razao_social, cpf_cnpj, apelido_nome_fantasia
            FROM emitentes
            WHERE nome_razao_social ILIKE @Termo OR cpf_cnpj ILIKE @Termo
               OR apelido_nome_fantasia ILIKE @Termo OR email ILIKE @Termo
            ORDER BY nome_razao_social
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql,
            new { Termo = $"%{termo}%", TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction);

        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<EmitentesResumo>();

        return new ResultadoPaginado<EmitentesResumo>(itens, total, pagina, tamanhoDaPagina);
    }
}
