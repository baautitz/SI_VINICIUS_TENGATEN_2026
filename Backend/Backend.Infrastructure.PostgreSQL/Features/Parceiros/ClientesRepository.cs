using Backend.Core.Common;
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

            SELECT id, nome_razao_social, cpf_cnpj, rg_ie, apelido_nome_fantasia,
                   endereco, telefone, email, limite_credito, ativo, criado_em,
                   observacao
            FROM clientes
            ORDER BY nome_razao_social
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
            SELECT c.id AS Id, c.nome_razao_social, c.cpf_cnpj, c.rg_ie, c.apelido_nome_fantasia,
                   c.endereco, c.telefone, c.email, c.limite_credito, c.ativo, c.criado_em,
                   c.atualizado_em, c.observacao,
                   b.id AS BairroId, b.bairro,
                   ci.id AS CidadeId, ci.cidade, ci.ddd,
                   e.id AS EstadoId, e.estado, e.uf,
                   p.id AS PaisId, p.pais, p.sigla_iso, p.ddi, p.moeda, p.simbolo_moeda
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
                    if (pais is not null && estado is not null) estado.Atualizar(estado.Estado, estado.Uf, pais);
                    if (estado is not null && cidade is not null) cidade.Atualizar(cidade.Cidade, cidade.Ddd, estado);
                    if (cidade is not null) bairro.Atualizar(bairro.Bairro, cidade);
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
            INSERT INTO clientes (nome_razao_social, cpf_cnpj, rg_ie, apelido_nome_fantasia,
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
            SET nome_razao_social = @NomeRazaoSocial, cpf_cnpj = @CpfCnpj,
                rg_ie = @RgIe, apelido_nome_fantasia = @ApelidoNomeFantasia,
                endereco = @Endereco, bairro_id = @BairroId, telefone = @Telefone,
                email = @Email, limite_credito = @LimiteCredito, ativo = @Ativo,
                atualizado_em = @AtualizadoEm, observacao = @Observacao
            WHERE id = @Id;";

        await _session.Connection.ExecuteAsync(
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
                cliente.Observacao
            },
            transaction: _session.Transaction
        );

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

            SELECT id, nome_razao_social, cpf_cnpj, apelido_nome_fantasia
            FROM clientes ORDER BY nome_razao_social
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
            WHERE nome_razao_social ILIKE @Termo OR cpf_cnpj ILIKE @Termo
               OR apelido_nome_fantasia ILIKE @Termo OR email ILIKE @Termo;

            SELECT id, nome_razao_social, cpf_cnpj, apelido_nome_fantasia
            FROM clientes
            WHERE nome_razao_social ILIKE @Termo OR cpf_cnpj ILIKE @Termo
               OR apelido_nome_fantasia ILIKE @Termo OR email ILIKE @Termo
            ORDER BY nome_razao_social
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql,
            new { Termo = $"%{termo}%", TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction);

        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<ClientesResumo>();

        return new ResultadoPaginado<ClientesResumo>(itens, total, pagina, tamanhoDaPagina);
    }
}
