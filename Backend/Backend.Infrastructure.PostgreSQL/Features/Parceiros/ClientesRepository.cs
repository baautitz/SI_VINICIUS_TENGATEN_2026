using Backend.Core.Common.Results;
using Backend.Core.Features.Localizacao.Entities;
using Backend.Core.Features.Parceiros.DTOs;
using Backend.Core.Features.Parceiros.Entities;
using Backend.Core.Features.Parceiros.Repositories;
using Backend.Infrastructure.PostgreSQL.Common;
using Dapper;

namespace Backend.Infrastructure.PostgreSQL.Features.Parceiros;

public class ClientesRepository : IClientesRepository
{
    private readonly DbSession _session;

    public ClientesRepository(DbSession session)
    {
        _session = session;
    }

    public async Task<ResultadoPaginado<Clientes>> ObterClientes(int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sql = @"
            SELECT COUNT(*) FROM clientes;

            SELECT id AS Id, nome_razaosocial AS NomeRazaoSocial, cpf_cnpj AS CpfCnpj, rg_ie AS RgIe, apelido_nomefantasia AS ApelidoNomeFantasia,
                   endereco AS Endereco, telefone AS Telefone, email AS Email, limite_credito AS LimiteCredito, ativo AS Ativo, criado_em AS CriadoEm,
                   observacao AS Observacao
            FROM clientes
            ORDER BY nome_razaosocial
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql,
            new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction
        );

        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<Clientes>();

        return new ResultadoPaginado<Clientes>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<Clientes?> ObterClientePorId(int id)
    {
        const string sql = @"
            SELECT c.id AS Id, c.nome_razaosocial AS NomeRazaoSocial, c.cpf_cnpj AS CpfCnpj, c.rg_ie AS RgIe, c.apelido_nomefantasia AS ApelidoNomeFantasia,
                   c.endereco AS Endereco, c.telefone AS Telefone, c.email AS Email, c.limite_credito AS LimiteCredito, c.ativo AS Ativo, c.criado_em AS CriadoEm,
                   c.observacao AS Observacao,
                   b.id AS BairroId, b.id AS Id, b.bairro,
                   ci.id AS CidadeId, ci.id AS Id, ci.cidade, ci.ddd,
                   e.id AS EstadoId, e.id AS Id, e.estado, e.uf,
                   p.id AS PaisId, p.id AS Id, p.pais, p.sigla_iso, p.ddi, p.moeda, p.simbolo_moeda
            FROM clientes c
            LEFT JOIN bairros b ON b.id = c.bairro_id
            LEFT JOIN cidades ci ON ci.id = b.cidade_id
            LEFT JOIN estados e ON e.id = ci.estado_id
            LEFT JOIN paises p ON p.id = e.pais_id
            WHERE c.id = @Id;";

        var result = await _session.Connection.QueryAsync<Clientes, Bairros, Cidades, Estados, Paises, Clientes>(
            sql,
            (cliente, bairro, cidade, estado, pais) =>
            {
                if (bairro is not null)
                {
                    if (pais is not null && estado is not null) estado.AtualizarResultado(estado.Estado, estado.Uf, pais);
                    if (estado is not null && cidade is not null) cidade.AtualizarResultado(cidade.Cidade, cidade.Ddd, estado);
                    if (cidade is not null) bairro.AtualizarResultado(bairro.Bairro, cidade);
                    cliente.AtualizarDados(cliente.NomeRazaoSocial, cliente.CpfCnpj, cliente.RgIe, cliente.ApelidoNomeFantasia, cliente.Endereco, bairro, cliente.Telefone, cliente.Email, cliente.LimiteCredito, cliente.Observacao);
                }
                return cliente;
            },
            new { Id = id },
            transaction: _session.Transaction,
            splitOn: "BairroId,CidadeId,EstadoId,PaisId"
        );

        return result.SingleOrDefault();
    }

    public async Task<Clientes> CriarCliente(Clientes cliente)
    {
        const string sql = @"
            INSERT INTO clientes (nome_razaosocial, cpf_cnpj, rg_ie, apelido_nomefantasia,
                                  endereco, bairro_id, telefone, email, limite_credito,
                                  ativo, criado_em, observacao)
            VALUES (@NomeRazaoSocial, @CpfCnpj, @RgIe, @ApelidoNomeFantasia,
                    @Endereco, @BairroId, @Telefone, @Email, @LimiteCredito,
                    @Ativo, @CriadoEm, @Observacao)
            RETURNING id;";

        var idGerado = await _session.Connection.ExecuteScalarAsync<int>(
            sql,
            new
            {
                cliente.NomeRazaoSocial,
                cliente.CpfCnpj,
                cliente.RgIe,
                cliente.ApelidoNomeFantasia,
                cliente.Endereco,
                BairroId = cliente.Bairro?.Id,
                cliente.Telefone,
                cliente.Email,
                cliente.LimiteCredito,
                cliente.Ativo,
                CriadoEm = DateTime.UtcNow,
                cliente.Observacao
            },
            transaction: _session.Transaction
        );

        cliente.Id = idGerado;
        return cliente;
    }

    public async Task<Clientes> AtualizarCliente(int id, Clientes cliente)
    {
        const string sql = @"
            UPDATE clientes
            SET nome_razaosocial = @NomeRazaoSocial, cpf_cnpj = @CpfCnpj,
                rg_ie = @RgIe, apelido_nomefantasia = @ApelidoNomeFantasia,
                endereco = @Endereco, bairro_id = @BairroId, telefone = @Telefone,
                email = @Email, limite_credito = @LimiteCredito, ativo = @Ativo,
                atualizado_em = @AtualizadoEm, observacao = @Observacao
            WHERE id = @Id;";

        var linhasAfetadas = await _session.Connection.ExecuteAsync(
            sql,
            new
            {
                Id = id,
                cliente.NomeRazaoSocial,
                cliente.CpfCnpj,
                cliente.RgIe,
                cliente.ApelidoNomeFantasia,
                cliente.Endereco,
                BairroId = cliente.Bairro?.Id,
                cliente.Telefone,
                cliente.Email,
                cliente.LimiteCredito,
                cliente.Ativo,
                AtualizadoEm = DateTime.UtcNow,
                cliente.Observacao
            },
            transaction: _session.Transaction
        );

        if (linhasAfetadas == 0)
            throw new Exception($"Falha ao atualizar: O cliente com ID {id} não foi encontrado ou não houve mudanças.");

        cliente.Id = id;
        return cliente;
    }

    public async Task<bool> DeletarCliente(int id)
    {
        const string sql = "DELETE FROM clientes WHERE id = @Id;";

        var linhasAfetadas = await _session.Connection.ExecuteAsync(
            sql, new { Id = id }, transaction: _session.Transaction);

        return linhasAfetadas > 0;
    }

    public async Task<ResultadoPaginado<ClientesResumo>> ObterClientesResumo(int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sql = @"
            SELECT COUNT(*) FROM clientes;

            SELECT id, nome_razaosocial AS NomeRazaoSocial, cpf_cnpj AS CpfCnpj, apelido_nomefantasia AS ApelidoNomeFantasia
            FROM clientes ORDER BY nome_razaosocial
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql, new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction);

        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<ClientesResumo>();

        return new ResultadoPaginado<ClientesResumo>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<ResultadoPaginado<ClientesResumo>> PesquisarClientes(string termo, int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sql = @"
            SELECT COUNT(*) FROM clientes
            WHERE nome_razaosocial ILIKE @Termo OR cpf_cnpj ILIKE @Termo
               OR apelido_nomefantasia ILIKE @Termo OR email ILIKE @Termo;

            SELECT id, nome_razaosocial AS NomeRazaoSocial, cpf_cnpj AS CpfCnpj, apelido_nomefantasia AS ApelidoNomeFantasia
            FROM clientes
            WHERE nome_razaosocial ILIKE @Termo OR cpf_cnpj ILIKE @Termo
               OR apelido_nomefantasia ILIKE @Termo OR email ILIKE @Termo
            ORDER BY nome_razaosocial
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql,
            new { Termo = $"%{termo}%", TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction);

        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<ClientesResumo>();

        return new ResultadoPaginado<ClientesResumo>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<bool> ExisteClienteCpfCnpj(string cpfCnpj, int? paisId, int? ignorarId = null)
    {
        var sql = @"
            SELECT COUNT(1)
            FROM clientes c
            WHERE c.cpf_cnpj = @CpfCnpj";

        if (ignorarId.HasValue)
        {
            sql += " AND c.id != @IgnorarId";
        }

        var count = await _session.Connection.ExecuteScalarAsync<int>(
            sql,
            new { CpfCnpj = cpfCnpj, IgnorarId = ignorarId },
            transaction: _session.Transaction);

        return count > 0;
    }
}
