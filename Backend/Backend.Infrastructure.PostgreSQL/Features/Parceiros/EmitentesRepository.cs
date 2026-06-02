using Backend.Core.Common.Results;
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

        const string sqlCount = "SELECT COUNT(*) FROM emitentes;";
        const string sqlData = @"
            SELECT e.id AS Id, e.tipo_pessoa AS TipoPessoa, e.nome_razaosocial AS NomeRazaoSocial, e.cpf_cnpj AS CpfCnpj, e.apelido_nomefantasia AS ApelidoNomeFantasia, e.endereco AS Endereco,
                   e.telefone AS Telefone, e.email AS Email, e.rg_ie AS RgIe, e.inscricao_municipal AS InscricaoMunicipal, e.regime_tributario AS RegimeTributario,
                   e.ativo AS Ativo, e.criado_em AS CriadoEm, e.observacao AS Observacao,
                   p.id AS PaisId, p.id AS Id, p.pais, p.sigla_iso, p.ddi, p.moeda, p.simbolo_moeda
            FROM emitentes e
            INNER JOIN paises p ON p.id = e.nacionalidade_id
            ORDER BY e.nome_razaosocial
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        var total = await _session.Connection.ExecuteScalarAsync<int>(sqlCount, transaction: _session.Transaction);

        var itens = await _session.Connection.QueryAsync<Emitentes, Paises, Emitentes>(
            sqlData,
            (emitente, pais) => {
                emitente.AtualizarDados(emitente.TipoPessoa, emitente.NomeRazaoSocial, emitente.CpfCnpj, pais!, emitente.ApelidoNomeFantasia, emitente.Endereco, null, emitente.Telefone, emitente.Email, emitente.RgIe, emitente.InscricaoMunicipal, emitente.RegimeTributario, emitente.Observacao);
                return emitente;
            },
            new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction,
            splitOn: "PaisId"
        );

        return new ResultadoPaginado<Emitentes>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<Emitentes?> ObterEmitentePorId(int id)
    {
        const string sql = @"
            SELECT e.id AS Id, e.tipo_pessoa AS TipoPessoa, e.nome_razaosocial AS NomeRazaoSocial, e.cpf_cnpj AS CpfCnpj, e.apelido_nomefantasia AS ApelidoNomeFantasia,
                   e.endereco AS Endereco, e.telefone AS Telefone, e.email AS Email, e.rg_ie AS RgIe, e.inscricao_municipal AS InscricaoMunicipal,
                   e.regime_tributario AS RegimeTributario, e.ativo AS Ativo, e.criado_em AS CriadoEm, e.observacao AS Observacao,
                   b.id AS BairroId, b.id AS Id, b.bairro,
                   c.id AS CidadeId, c.id AS Id, c.cidade, c.ddd,
                   st.id AS EstadoId, st.id AS Id, st.estado, st.uf,
                   p.id AS PaisId, p.id AS Id, p.pais, p.sigla_iso, p.ddi, p.moeda, p.simbolo_moeda
            FROM emitentes e
            INNER JOIN paises p ON p.id = e.nacionalidade_id
            LEFT JOIN bairros b ON b.id = e.bairro_id
            LEFT JOIN cidades c ON c.id = b.cidade_id
            LEFT JOIN estados st ON st.id = c.estado_id
            WHERE e.id = @Id;";

        var result = await _session.Connection.QueryAsync<Emitentes, Bairros, Cidades, Estados, Paises, Emitentes>(
            sql,
            (emitente, bairro, cidade, estado, pais) =>
            {
                if (pais is not null && estado is not null) estado.AtualizarResultado(estado.Estado, estado.Uf, pais);
                if (estado is not null && cidade is not null) cidade.AtualizarResultado(cidade.Cidade, cidade.Ddd, estado);
                if (cidade is not null && bairro is not null) bairro.AtualizarResultado(bairro.Bairro, cidade);
                
                emitente.AtualizarDados(emitente.TipoPessoa, emitente.NomeRazaoSocial, emitente.CpfCnpj, pais!, emitente.ApelidoNomeFantasia, emitente.Endereco, bairro, emitente.Telefone, emitente.Email, emitente.RgIe, emitente.InscricaoMunicipal, emitente.RegimeTributario, emitente.Observacao);
                
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
            INSERT INTO emitentes (tipo_pessoa, nome_razaosocial, cpf_cnpj, apelido_nomefantasia, endereco,
                                   bairro_id, nacionalidade_id, telefone, email, rg_ie, inscricao_municipal,
                                   regime_tributario, ativo, criado_em, observacao)
            VALUES (@TipoPessoa, @NomeRazaoSocial, @CpfCnpj, @ApelidoNomeFantasia, @Endereco,
                    @BairroId, @NacionalidadeId, @Telefone, @Email, @RgIe, @InscricaoMunicipal,
                    @RegimeTributario, @Ativo, @CriadoEm, @Observacao)
            RETURNING id;";

        var idGerado = await _session.Connection.ExecuteScalarAsync<int>(
            sql,
            new
            {
                TipoPessoa = emitente.TipoPessoa.ToString(),
                emitente.NomeRazaoSocial,
                emitente.CpfCnpj,
                emitente.ApelidoNomeFantasia,
                emitente.Endereco,
                BairroId = emitente.Bairro?.Id,
                NacionalidadeId = emitente.Nacionalidade.Id,
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
            SET tipo_pessoa = @TipoPessoa, nome_razaosocial = @NomeRazaoSocial, cpf_cnpj = @CpfCnpj,
                apelido_nomefantasia = @ApelidoNomeFantasia, endereco = @Endereco,
                bairro_id = @BairroId, nacionalidade_id = @NacionalidadeId, telefone = @Telefone, email = @Email,
                rg_ie = @RgIe, inscricao_municipal = @InscricaoMunicipal,
                regime_tributario = @RegimeTributario, ativo = @Ativo,
                atualizado_em = @AtualizadoEm, observacao = @Observacao
            WHERE id = @Id;";

        await _session.Connection.ExecuteAsync(
            sql,
            new
            {
                Id = id,
                TipoPessoa = emitente.TipoPessoa.ToString(),
                emitente.NomeRazaoSocial,
                emitente.CpfCnpj,
                emitente.ApelidoNomeFantasia,
                emitente.Endereco,
                BairroId = emitente.Bairro?.Id,
                NacionalidadeId = emitente.Nacionalidade.Id,
                emitente.Telefone,
                emitente.Email,
                emitente.RgIe,
                emitente.InscricaoMunicipal,
                emitente.RegimeTributario,
                emitente.Ativo,
                AtualizadoEm = DateTime.UtcNow,
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

            SELECT id, tipo_pessoa AS TipoPessoa, nome_razaosocial AS NomeRazaoSocial, cpf_cnpj AS CpfCnpj, apelido_nomefantasia AS ApelidoNomeFantasia, nacionalidade_id AS NacionalidadeId
            FROM emitentes ORDER BY nome_razaosocial
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
            WHERE nome_razaosocial ILIKE @Termo OR cpf_cnpj ILIKE @Termo
               OR apelido_nomefantasia ILIKE @Termo OR email ILIKE @Termo;

            SELECT id, tipo_pessoa AS TipoPessoa, nome_razaosocial AS NomeRazaoSocial, cpf_cnpj AS CpfCnpj, apelido_nomefantasia AS ApelidoNomeFantasia, nacionalidade_id AS NacionalidadeId
            FROM emitentes
            WHERE nome_razaosocial ILIKE @Termo OR cpf_cnpj ILIKE @Termo
               OR apelido_nomefantasia ILIKE @Termo OR email ILIKE @Termo
            ORDER BY nome_razaosocial
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql,
            new { Termo = $"%{termo}%", TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction);

        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<EmitentesResumo>();

        return new ResultadoPaginado<EmitentesResumo>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<bool> ExisteEmitenteCpfCnpj(string cpfCnpj, int nacionalidadeId, int? ignorarId = null)
    {
        var sql = @"
            SELECT COUNT(1)
            FROM emitentes em
            WHERE em.cpf_cnpj = @CpfCnpj AND em.nacionalidade_id = @NacionalidadeId";

        if (ignorarId.HasValue)
        {
            sql += " AND em.id != @IgnorarId";
        }

        var count = await _session.Connection.ExecuteScalarAsync<int>(
            sql,
            new { CpfCnpj = cpfCnpj, NacionalidadeId = nacionalidadeId, IgnorarId = ignorarId },
            transaction: _session.Transaction);

        return count > 0;
    }
}
