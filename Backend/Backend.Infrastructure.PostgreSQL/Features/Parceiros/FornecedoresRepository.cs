using System.Linq;
using Backend.Core.Common.Results;
using Backend.Core.Features.Localizacao.Entities;
using Backend.Core.Features.Parceiros.Entities;
using Backend.Core.Features.Parceiros.Repositories;
using Backend.Infrastructure.PostgreSQL.Common;
using Dapper;

namespace Backend.Infrastructure.PostgreSQL.Features.Parceiros;

public class FornecedoresRepository : IFornecedoresRepository
{
    private readonly DbSession _session;

    public FornecedoresRepository(DbSession session)
    {
        _session = session;
    }

    public async Task<ResultadoPaginado<Fornecedores>> ObterFornecedores(int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sqlCount = "SELECT COUNT(*) FROM fornecedores;";
        const string sqlData = @"
            SELECT f.id AS Id, f.tipo_pessoa AS TipoPessoa, f.nome_razaosocial AS NomeRazaosocial, f.cpf_cnpj AS CpfCnpj, f.rg_ie AS RgIe, f.apelido_nomefantasia AS ApelidoNomefantasia,
                   f.endereco AS Endereco, f.telefone AS Telefone, f.email AS Email, f.ativo AS Ativo, f.criado_em AS CriadoEm,
                   f.observacao AS Observacao,
                   p.id AS PaisId, p.id AS Id, p.pais, p.sigla_iso, p.ddi, p.moeda, p.simbolo_moeda
            FROM fornecedores f
            INNER JOIN paises p ON p.id = f.nacionalidade_id
            ORDER BY f.nome_razaosocial
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        var total = await _session.Connection.ExecuteScalarAsync<int>(sqlCount, transaction: _session.Transaction);

        var itens = await _session.Connection.QueryAsync<Fornecedores, Paises, Fornecedores>(
            sqlData,
            (fornecedor, pais) =>
            {
                fornecedor.Atualizar(fornecedor.TipoPessoa, fornecedor.NomeRazaosocial, fornecedor.CpfCnpj, pais!, fornecedor.RgIe, fornecedor.ApelidoNomefantasia, fornecedor.Endereco, null, fornecedor.Telefone, fornecedor.Email, fornecedor.Observacao);
                return fornecedor;
            },
            new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction,
            splitOn: "PaisId"
        );

        return new ResultadoPaginado<Fornecedores>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<Fornecedores?> ObterFornecedorPorId(int id)
    {
        const string sql = @"
            SELECT f.id AS Id, f.tipo_pessoa AS TipoPessoa, f.nome_razaosocial AS NomeRazaosocial, f.cpf_cnpj AS CpfCnpj, f.rg_ie AS RgIe, f.apelido_nomefantasia AS ApelidoNomefantasia,
                   f.endereco AS Endereco, f.telefone AS Telefone, f.email AS Email, f.ativo AS Ativo, f.criado_em AS CriadoEm,
                   f.observacao AS Observacao,
                   b.id AS BairroId, b.id AS Id, b.bairro,
                   ci.id AS CidadeId, ci.id AS Id, ci.cidade, ci.ddd,
                   e.id AS EstadoId, e.id AS Id, e.estado, e.uf,
                   p.id AS PaisId, p.id AS Id, p.pais, p.sigla_iso, p.ddi, p.moeda, p.simbolo_moeda
            FROM fornecedores f
            INNER JOIN paises p ON p.id = f.nacionalidade_id
            LEFT JOIN bairros b ON b.id = f.bairro_id
            LEFT JOIN cidades ci ON ci.id = b.cidade_id
            LEFT JOIN estados e ON e.id = ci.estado_id
            WHERE f.id = @Id;";

        var result = await _session.Connection.QueryAsync<Fornecedores, Bairros, Cidades, Estados, Paises, Fornecedores>(
            sql,
            (fornecedor, bairro, city, state, country) =>
            {
                if (country is not null && state is not null) state.AtualizarResultado(state.Estado, state.Uf, country);
                if (state is not null && city is not null) city.AtualizarResultado(city.Cidade, city.Ddd, state);
                if (city is not null && bairro is not null) bairro.AtualizarResultado(bairro.Bairro, city);

                fornecedor.Atualizar(fornecedor.TipoPessoa, fornecedor.NomeRazaosocial, fornecedor.CpfCnpj, country!, fornecedor.RgIe, fornecedor.ApelidoNomefantasia, fornecedor.Endereco, bairro, fornecedor.Telefone, fornecedor.Email, fornecedor.Observacao);

                return fornecedor;
            },
            new { Id = id },
            transaction: _session.Transaction,
            splitOn: "BairroId,CidadeId,EstadoId,PaisId"
        );

        return result.SingleOrDefault();
    }

    public async Task<Fornecedores> CriarFornecedor(Fornecedores fornecedor)
    {
        const string sql = @"
            INSERT INTO fornecedores (tipo_pessoa, nome_razaosocial, cpf_cnpj, rg_ie, apelido_nomefantasia,
                                      endereco, bairro_id, nacionalidade_id, telefone, email,
                                      ativo, criado_em, observacao)
            VALUES (@TipoPessoa::tipo_pessoa, @NomeRazaosocial, @CpfCnpj, @RgIe, @ApelidoNomefantasia,
                    @Endereco, @BairroId, @NacionalidadeId, @Telefone, @Email,
                    @Ativo, @CriadoEm, @Observacao)
            RETURNING id;";

        var idGerado = await _session.Connection.ExecuteScalarAsync<int>(
            sql,
            new
            {
                TipoPessoa = fornecedor.TipoPessoa.ToString(),
                fornecedor.NomeRazaosocial,
                fornecedor.CpfCnpj,
                fornecedor.RgIe,
                fornecedor.ApelidoNomefantasia,
                fornecedor.Endereco,
                BairroId = fornecedor.Bairro?.Id,
                NacionalidadeId = fornecedor.Nacionalidade.Id,
                fornecedor.Telefone,
                fornecedor.Email,
                fornecedor.Ativo,
                CriadoEm = DateTime.UtcNow,
                fornecedor.Observacao
            },
            transaction: _session.Transaction
        );

        fornecedor.Id = idGerado;

        return fornecedor;
    }

    public async Task<Fornecedores> AtualizarFornecedor(int id, Fornecedores fornecedor)
    {
        const string sql = @"
            UPDATE fornecedores
            SET tipo_pessoa = @TipoPessoa::tipo_pessoa, nome_razaosocial = @NomeRazaosocial, cpf_cnpj = @CpfCnpj,
                rg_ie = @RgIe, apelido_nomefantasia = @ApelidoNomefantasia,
                endereco = @Endereco, bairro_id = @BairroId, nacionalidade_id = @NacionalidadeId,
                telefone = @Telefone, email = @Email, ativo = @Ativo,
                atualizado_em = @AtualizadoEm, observacao = @Observacao
            WHERE id = @Id;";

        await _session.Connection.ExecuteAsync(
            sql,
            new
            {
                Id = id,
                TipoPessoa = fornecedor.TipoPessoa.ToString(),
                fornecedor.NomeRazaosocial,
                fornecedor.CpfCnpj,
                fornecedor.RgIe,
                fornecedor.ApelidoNomefantasia,
                fornecedor.Endereco,
                BairroId = fornecedor.Bairro?.Id,
                NacionalidadeId = fornecedor.Nacionalidade.Id,
                fornecedor.Telefone,
                fornecedor.Email,
                fornecedor.Ativo,
                AtualizadoEm = DateTime.UtcNow,
                fornecedor.Observacao
            },
            transaction: _session.Transaction
        );

        fornecedor.Id = id;

        return fornecedor;
    }

    public async Task<bool> DeletarFornecedor(int id)
    {
        const string sql = "DELETE FROM fornecedores WHERE id = @Id;";

        var affectedRows = await _session.Connection.ExecuteAsync(
            sql,
            new { Id = id },
            transaction: _session.Transaction
        );

        return affectedRows > 0;
    }

    public async Task<ResultadoPaginado<Fornecedores>> PesquisarFornecedores(string termo, int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sqlCount = @"
            SELECT COUNT(*) FROM fornecedores
            WHERE nome_razaosocial ILIKE @Termo OR cpf_cnpj ILIKE @Termo
               OR apelido_nomefantasia ILIKE @Termo OR email ILIKE @Termo;";

        const string sqlData = @"
            SELECT f.id AS Id, f.tipo_pessoa AS TipoPessoa, f.nome_razaosocial AS NomeRazaosocial, f.cpf_cnpj AS CpfCnpj, f.rg_ie AS RgIe, f.apelido_nomefantasia AS ApelidoNomefantasia,
                   f.endereco AS Endereco, f.telefone AS Telefone, f.email AS Email, f.ativo AS Ativo, f.criado_em AS CriadoEm,
                   f.observacao AS Observacao,
                   b.id AS BairroId, b.id AS Id, b.bairro,
                   ci.id AS CidadeId, ci.id AS Id, ci.cidade, ci.ddd,
                   e.id AS EstadoId, e.id AS Id, e.estado, e.uf,
                   p.id AS PaisId, p.id AS Id, p.pais, p.sigla_iso, p.ddi, p.moeda, p.simbolo_moeda
            FROM fornecedores f
            INNER JOIN paises p ON p.id = f.nacionalidade_id
            LEFT JOIN bairros b ON b.id = f.bairro_id
            LEFT JOIN cidades ci ON ci.id = b.cidade_id
            LEFT JOIN estados e ON e.id = ci.estado_id
            WHERE f.nome_razaosocial ILIKE @Termo OR f.cpf_cnpj ILIKE @Termo
               OR f.apelido_nomefantasia ILIKE @Termo OR f.email ILIKE @Termo
            ORDER BY f.nome_razaosocial
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        var total = await _session.Connection.ExecuteScalarAsync<int>(sqlCount, new { Termo = $"%{termo}%" }, transaction: _session.Transaction);

        var itens = await _session.Connection.QueryAsync<Fornecedores, Bairros, Cidades, Estados, Paises, Fornecedores>(
            sqlData,
            (fornecedor, bairro, city, state, country) =>
            {
                if (country is not null && state is not null) state.AtualizarResultado(state.Estado, state.Uf, country);
                if (state is not null && city is not null) city.AtualizarResultado(city.Cidade, city.Ddd, state);
                if (city is not null && bairro is not null) bairro.AtualizarResultado(bairro.Bairro, city);

                fornecedor.Atualizar(fornecedor.TipoPessoa, fornecedor.NomeRazaosocial, fornecedor.CpfCnpj, country!, fornecedor.RgIe, fornecedor.ApelidoNomefantasia, fornecedor.Endereco, bairro, fornecedor.Telefone, fornecedor.Email, fornecedor.Observacao);
                return fornecedor;
            },
            new { Termo = $"%{termo}%", TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction,
            splitOn: "BairroId,CidadeId,EstadoId,PaisId"
        );

        return new ResultadoPaginado<Fornecedores>(itens.ToList(), total, pagina, tamanhoDaPagina);
    }

    public async Task<bool> ExisteFornecedorCpfCnpj(string cpfCnpj, int nacionalidadeId, int? ignorarId = null)
    {
        var sql = @"
            SELECT COUNT(1)
            FROM fornecedores f
            WHERE f.cpf_cnpj = @CpfCnpj AND f.nacionalidade_id = @NacionalidadeId";

        if (ignorarId.HasValue)
        {
            sql += " AND f.id != @IgnorarId";
        }

        var count = await _session.Connection.ExecuteScalarAsync<int>(
            sql,
            new { CpfCnpj = cpfCnpj, NacionalidadeId = nacionalidadeId, IgnorarId = ignorarId },
            transaction: _session.Transaction);

        return count > 0;
    }
}
