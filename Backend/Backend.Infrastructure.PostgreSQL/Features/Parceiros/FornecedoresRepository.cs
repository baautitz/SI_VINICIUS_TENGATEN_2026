using Backend.Core.Common.Results;
using Backend.Core.Features.Localizacao.Entities;
using Backend.Core.Features.Parceiros.DTOs;
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

        const string sql = @"
            SELECT COUNT(*) FROM fornecedores;

            SELECT id, nome_razaosocial AS NomeRazaosocial, cpf_cnpj, rg_ie, apelido_nomefantasia AS ApelidoNomefantasia,
                   endereco, telefone, email, ativo, criado_em,
                   observacao
            FROM fornecedores
            ORDER BY nome_razaosocial
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql,
            new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction
        );

        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<Fornecedores>();

        return new ResultadoPaginado<Fornecedores>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<Fornecedores?> ObterFornecedorPorId(int id)
    {
        const string sql = @"
            SELECT f.id AS Id, f.nome_razaosocial AS NomeRazaosocial, f.cpf_cnpj, f.rg_ie, f.apelido_nomefantasia AS ApelidoNomefantasia,
                   f.endereco, f.telefone, f.email, f.ativo, f.criado_em,
                   f.observacao,
                   b.id AS BairroId, b.id AS Id, b.bairro,
                   ci.id AS CidadeId, ci.id AS Id, ci.cidade, ci.ddd,
                   e.id AS EstadoId, e.id AS Id, e.estado, e.uf,
                   p.id AS PaisId, p.id AS Id, p.pais, p.sigla_iso, p.ddi, p.moeda, p.simbolo_moeda
            FROM fornecedores f
            LEFT JOIN bairros b ON b.id = f.bairro_id
            LEFT JOIN cidades ci ON ci.id = b.cidade_id
            LEFT JOIN estados e ON e.id = ci.estado_id
            LEFT JOIN paises p ON p.id = e.pais_id
            WHERE f.id = @Id;";

        var result = await _session.Connection.QueryAsync<Fornecedores, Bairros, Cidades, Estados, Paises, Fornecedores>(
            sql,
            (fornecedor, bairro, cidade, estado, pais) =>
            {
                if (bairro is not null)
                {
                    if (pais is not null && estado is not null) estado.AtualizarResultado(estado.Estado, estado.Uf, pais);
                    if (estado is not null && cidade is not null) cidade.AtualizarResultado(cidade.Cidade, cidade.Ddd, estado);
                    if (cidade is not null) bairro.AtualizarResultado(bairro.Bairro, cidade);
                    fornecedor.Atualizar(fornecedor.NomeRazaosocial, fornecedor.CpfCnpj, fornecedor.RgIe, fornecedor.ApelidoNomefantasia, fornecedor.Endereco, bairro, fornecedor.Telefone, fornecedor.Email, fornecedor.Observacao);
                }
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
            INSERT INTO fornecedores (nome_razaosocial, cpf_cnpj, rg_ie, apelido_nomefantasia,
                                      endereco, bairro_id, telefone, email,
                                      ativo, criado_em, observacao)
            VALUES (@NomeRazaosocial, @CpfCnpj, @RgIe, @ApelidoNomefantasia,
                    @Endereco, @BairroId, @Telefone, @Email,
                    @Ativo, @CriadoEm, @Observacao)
            RETURNING id;";

        var idGerado = await _session.Connection.ExecuteScalarAsync<int>(
            sql,
            new
            {
                fornecedor.NomeRazaosocial,
                fornecedor.CpfCnpj,
                fornecedor.RgIe,
                fornecedor.ApelidoNomefantasia,
                fornecedor.Endereco,
                BairroId = fornecedor.Bairro?.Id,
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
            SET nome_razaosocial = @NomeRazaosocial, cpf_cnpj = @CpfCnpj,
                rg_ie = @RgIe, apelido_nomefantasia = @ApelidoNomefantasia,
                endereco = @Endereco, bairro_id = @BairroId, telefone = @Telefone,
                email = @Email, ativo = @Ativo,
                observacao = @Observacao
            WHERE id = @Id;";

        await _session.Connection.ExecuteAsync(
            sql,
            new
            {
                Id = id,
                fornecedor.NomeRazaosocial,
                fornecedor.CpfCnpj,
                fornecedor.RgIe,
                fornecedor.ApelidoNomefantasia,
                fornecedor.Endereco,
                BairroId = fornecedor.Bairro?.Id,
                fornecedor.Telefone,
                fornecedor.Email,
                fornecedor.Ativo,
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

    public async Task<ResultadoPaginado<FornecedoresResumo>> ObterFornecedoresResumo(int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sql = @"
            SELECT COUNT(*) FROM fornecedores;

            SELECT id, nome_razaosocial AS NomeRazaoSocial, cpf_cnpj AS CpfCnpj, apelido_nomefantasia AS ApelidoNomeFantasia
            FROM fornecedores
            ORDER BY nome_razaosocial
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql,
            new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction);

        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<FornecedoresResumo>();

        return new ResultadoPaginado<FornecedoresResumo>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<ResultadoPaginado<FornecedoresResumo>> PesquisarFornecedores(string termo, int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sql = @"
            SELECT COUNT(*) FROM fornecedores
            WHERE nome_razaosocial ILIKE @Termo OR cpf_cnpj ILIKE @Termo
               OR apelido_nomefantasia ILIKE @Termo OR email ILIKE @Termo;

            SELECT id, nome_razaosocial AS NomeRazaoSocial, cpf_cnpj AS CpfCnpj, apelido_nomefantasia AS ApelidoNomeFantasia
            FROM fornecedores
            WHERE nome_razaosocial ILIKE @Termo OR cpf_cnpj ILIKE @Termo
               OR apelido_nomefantasia ILIKE @Termo OR email ILIKE @Termo
            ORDER BY nome_razaosocial
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql,
            new { Termo = $"%{termo}%", TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction);

        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<FornecedoresResumo>();

        return new ResultadoPaginado<FornecedoresResumo>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<bool> ExisteFornecedorCpfCnpj(string cpfCnpj, int? paisId, int? ignorarId = null)
    {
        var sql = @"
            SELECT COUNT(1)
            FROM fornecedores f
            WHERE f.cpf_cnpj = @CpfCnpj";

        if (ignorarId.HasValue)
        {
            sql += " AND f.id != @IgnorarId";
        }

        var count = await _session.Connection.ExecuteScalarAsync<int>(
            sql,
            new { CpfCnpj = cpfCnpj, IgnorarId = ignorarId },
            transaction: _session.Transaction);

        return count > 0;
    }
}