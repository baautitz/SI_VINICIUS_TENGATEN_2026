using System.Linq;
using Backend.Core.Common.Results;
using Backend.Core.Features.Localizacao.Entities;
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

        const string sqlCount = "SELECT COUNT(*) FROM clientes;";
        const string sqlData = @"
            SELECT c.id AS Id, c.tipo_pessoa AS TipoPessoa, c.nome_razaosocial AS NomeRazaoSocial, c.cpf_cnpj AS CpfCnpj, c.rg_ie AS RgIe, c.apelido_nomefantasia AS ApelidoNomeFantasia,
                   c.logradouro AS Logradouro, c.numero AS Numero, c.sexo AS Sexo, c.data_nascimento AS DataNascimento, c.telefone AS Telefone, c.email AS Email, c.limite_credito AS LimiteCredito, c.ativo AS Ativo, c.criado_em AS CriadoEm,
                   c.observacao AS Observacao,
                   b.id AS BairroId, b.id AS Id, b.bairro,
                   ci.id AS CidadeId, ci.id AS Id, ci.cidade, ci.ddd,
                   e.id AS EstadoId, e.id AS Id, e.estado, e.uf,
                   pe.id AS PaisEstadoId, pe.id AS Id, pe.pais, pe.codigo_iso_pais, pe.ddi, pe.codigo_iso_moeda, pe.simbolo_moeda,
                   p.id AS PaisId, p.id AS Id, p.pais, p.codigo_iso_pais, p.ddi, p.codigo_iso_moeda, p.simbolo_moeda
            FROM clientes c
            INNER JOIN paises p ON p.id = c.nacionalidade_id
            LEFT JOIN bairros b ON b.id = c.bairro_id
            LEFT JOIN cidades ci ON ci.id = b.cidade_id
            LEFT JOIN estados e ON e.id = ci.estado_id
            LEFT JOIN paises pe ON pe.id = e.pais_id
            ORDER BY c.criado_em DESC, c.id DESC
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        var total = await _session.Connection.ExecuteScalarAsync<int>(sqlCount, transaction: _session.Transaction);

        var itens = await _session.Connection.QueryAsync<Clientes, Bairros, Cidades, Estados, Paises, Paises, Clientes>(
            sqlData,
            (cliente, bairro, cidade, estado, paisEstado, pais) =>
            {
                if (paisEstado is not null && estado is not null) estado.AtualizarResultado(estado.Estado, estado.Uf, paisEstado);
                if (estado is not null && cidade is not null) cidade.AtualizarResultado(cidade.Cidade, cidade.Ddd, estado);
                if (cidade is not null && bairro is not null) bairro.AtualizarResultado(bairro.Bairro, cidade);

                cliente.AtualizarDados(cliente.TipoPessoa, cliente.NomeRazaoSocial, cliente.CpfCnpj, pais!, cliente.RgIe, cliente.ApelidoNomeFantasia, cliente.Logradouro, cliente.Numero, bairro, cliente.Telefone, cliente.Email, cliente.LimiteCredito, cliente.Observacao, cliente.Sexo, cliente.DataNascimento);
                return cliente;
            },
            new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction,
            splitOn: "BairroId,CidadeId,EstadoId,PaisEstadoId,PaisId"
        );

        return new ResultadoPaginado<Clientes>(itens.ToList(), total, pagina, tamanhoDaPagina);
    }

    public async Task<Clientes?> ObterClientePorId(int id)
    {
        const string sql = @"
            SELECT c.id AS Id, c.tipo_pessoa AS TipoPessoa, c.nome_razaosocial AS NomeRazaoSocial, c.cpf_cnpj AS CpfCnpj, c.rg_ie AS RgIe, c.apelido_nomefantasia AS ApelidoNomeFantasia,
                   c.logradouro AS Logradouro, c.numero AS Numero, c.sexo AS Sexo, c.data_nascimento AS DataNascimento, c.telefone AS Telefone, c.email AS Email, c.limite_credito AS LimiteCredito, c.ativo AS Ativo, c.criado_em AS CriadoEm,
                   c.observacao AS Observacao,
                   b.id AS BairroId, b.id AS Id, b.bairro,
                   ci.id AS CidadeId, ci.id AS Id, ci.cidade, ci.ddd,
                   e.id AS EstadoId, e.id AS Id, e.estado, e.uf,
                   pe.id AS PaisEstadoId, pe.id AS Id, pe.pais, pe.codigo_iso_pais, pe.ddi, pe.codigo_iso_moeda, pe.simbolo_moeda,
                   p.id AS PaisId, p.id AS Id, p.pais, p.codigo_iso_pais, p.ddi, p.codigo_iso_moeda, p.simbolo_moeda
            FROM clientes c
            INNER JOIN paises p ON p.id = c.nacionalidade_id
            LEFT JOIN bairros b ON b.id = c.bairro_id
            LEFT JOIN cidades ci ON ci.id = b.cidade_id
            LEFT JOIN estados e ON e.id = ci.estado_id
            LEFT JOIN paises pe ON pe.id = e.pais_id
            WHERE c.id = @Id;";

        var result = await _session.Connection.QueryAsync<Clientes, Bairros, Cidades, Estados, Paises, Paises, Clientes>(
            sql,
            (cliente, bairro, cidade, estado, paisEstado, pais) =>
            {
                if (paisEstado is not null && estado is not null) estado.AtualizarResultado(estado.Estado, estado.Uf, paisEstado);
                if (estado is not null && cidade is not null) cidade.AtualizarResultado(cidade.Cidade, cidade.Ddd, estado);
                if (cidade is not null && bairro is not null) bairro.AtualizarResultado(bairro.Bairro, cidade);

                cliente.AtualizarDados(cliente.TipoPessoa, cliente.NomeRazaoSocial, cliente.CpfCnpj, pais!, cliente.RgIe, cliente.ApelidoNomeFantasia, cliente.Logradouro, cliente.Numero, bairro, cliente.Telefone, cliente.Email, cliente.LimiteCredito, cliente.Observacao, cliente.Sexo, cliente.DataNascimento);

                return cliente;
            },
            new { Id = id },
            transaction: _session.Transaction,
            splitOn: "BairroId,CidadeId,EstadoId,PaisEstadoId,PaisId"
        );

        return result.SingleOrDefault();
    }

    public async Task<Clientes> CriarCliente(Clientes cliente)
    {
        const string sql = @"
            INSERT INTO clientes (tipo_pessoa, nome_razaosocial, cpf_cnpj, rg_ie, apelido_nomefantasia,
                                  logradouro, numero, sexo, data_nascimento, bairro_id, nacionalidade_id, telefone, email, limite_credito,
                                  ativo, criado_em, observacao)
            VALUES (@TipoPessoa::tipo_pessoa, @NomeRazaoSocial, @CpfCnpj, @RgIe, @ApelidoNomeFantasia,
                    @Logradouro, @Numero, @Sexo, @DataNascimento, @BairroId, @NacionalidadeId, @Telefone, @Email, @LimiteCredito,
                    @Ativo, @CriadoEm, @Observacao)
            RETURNING id;";

        var idGerado = await _session.Connection.ExecuteScalarAsync<int>(
            sql,
            new
            {
                TipoPessoa = cliente.TipoPessoa.ToString(),
                cliente.NomeRazaoSocial,
                cliente.CpfCnpj,
                cliente.RgIe,
                cliente.ApelidoNomeFantasia,
                cliente.Logradouro,
                cliente.Numero,
                cliente.Sexo,
                cliente.DataNascimento,
                BairroId = cliente.Bairro?.Id,
                NacionalidadeId = cliente.Nacionalidade.Id,
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
            SET tipo_pessoa = @TipoPessoa::tipo_pessoa, nome_razaosocial = @NomeRazaoSocial, cpf_cnpj = @CpfCnpj,
                rg_ie = @RgIe, apelido_nomefantasia = @ApelidoNomeFantasia,
                logradouro = @Logradouro, numero = @Numero, sexo = @Sexo, data_nascimento = @DataNascimento, bairro_id = @BairroId, nacionalidade_id = @NacionalidadeId,
                telefone = @Telefone, email = @Email, limite_credito = @LimiteCredito, 
                ativo = @Ativo, atualizado_em = @AtualizadoEm, observacao = @Observacao
            WHERE id = @Id;";

        var linhasAfetadas = await _session.Connection.ExecuteAsync(
            sql,
            new
            {
                Id = id,
                TipoPessoa = cliente.TipoPessoa.ToString(),
                cliente.NomeRazaoSocial,
                cliente.CpfCnpj,
                cliente.RgIe,
                cliente.ApelidoNomeFantasia,
                cliente.Logradouro,
                cliente.Numero,
                cliente.Sexo,
                cliente.DataNascimento,
                BairroId = cliente.Bairro?.Id,
                NacionalidadeId = cliente.Nacionalidade.Id,
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

    public async Task<ResultadoPaginado<Clientes>> PesquisarClientes(string termo, int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sqlCount = @"
            SELECT COUNT(*) FROM clientes
            WHERE nome_razaosocial ILIKE @Termo OR cpf_cnpj ILIKE @Termo
               OR apelido_nomefantasia ILIKE @Termo OR email ILIKE @Termo;";

        const string sqlData = @"
            SELECT c.id AS Id, c.tipo_pessoa AS TipoPessoa, c.nome_razaosocial AS NomeRazaoSocial, c.cpf_cnpj AS CpfCnpj, c.rg_ie AS RgIe, c.apelido_nomefantasia AS ApelidoNomeFantasia,
                   c.logradouro AS Logradouro, c.numero AS Numero, c.sexo AS Sexo, c.data_nascimento AS DataNascimento, c.telefone AS Telefone, c.email AS Email, c.limite_credito AS LimiteCredito, c.ativo AS Ativo, c.criado_em AS CriadoEm,
                   c.observacao AS Observacao,
                   b.id AS BairroId, b.id AS Id, b.bairro,
                   ci.id AS CidadeId, ci.id AS Id, ci.cidade, ci.ddd,
                   e.id AS EstadoId, e.id AS Id, e.estado, e.uf,
                   pe.id AS PaisEstadoId, pe.id AS Id, pe.pais, pe.codigo_iso_pais, pe.ddi, pe.codigo_iso_moeda, pe.simbolo_moeda,
                   p.id AS PaisId, p.id AS Id, p.pais, p.codigo_iso_pais, p.ddi, p.codigo_iso_moeda, p.simbolo_moeda
            FROM clientes c
            INNER JOIN paises p ON p.id = c.nacionalidade_id
            LEFT JOIN bairros b ON b.id = c.bairro_id
            LEFT JOIN cidades ci ON ci.id = b.cidade_id
            LEFT JOIN estados e ON e.id = ci.estado_id
            LEFT JOIN paises pe ON pe.id = e.pais_id
            WHERE c.nome_razaosocial ILIKE @Termo OR c.cpf_cnpj ILIKE @Termo
               OR c.apelido_nomefantasia ILIKE @Termo OR c.email ILIKE @Termo
            ORDER BY c.criado_em DESC, c.id DESC
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        var total = await _session.Connection.ExecuteScalarAsync<int>(sqlCount, new { Termo = $"%{termo}%" }, transaction: _session.Transaction);

        var itens = await _session.Connection.QueryAsync<Clientes, Bairros, Cidades, Estados, Paises, Paises, Clientes>(
            sqlData,
            (cliente, bairro, cidade, estado, paisEstado, pais) =>
            {
                if (paisEstado is not null && estado is not null) estado.AtualizarResultado(estado.Estado, estado.Uf, paisEstado);
                if (estado is not null && cidade is not null) cidade.AtualizarResultado(cidade.Cidade, cidade.Ddd, estado);
                if (cidade is not null && bairro is not null) bairro.AtualizarResultado(bairro.Bairro, cidade);

                cliente.AtualizarDados(cliente.TipoPessoa, cliente.NomeRazaoSocial, cliente.CpfCnpj, pais!, cliente.RgIe, cliente.ApelidoNomeFantasia, cliente.Logradouro, cliente.Numero, bairro, cliente.Telefone, cliente.Email, cliente.LimiteCredito, cliente.Observacao, cliente.Sexo, cliente.DataNascimento);
                return cliente;
            },
            new { Termo = $"%{termo}%", TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction,
            splitOn: "BairroId,CidadeId,EstadoId,PaisEstadoId,PaisId"
        );

        return new ResultadoPaginado<Clientes>(itens.ToList(), total, pagina, tamanhoDaPagina);
    }

    public async Task<bool> ExisteClienteCpfCnpj(string cpfCnpj, int nacionalidadeId, int? ignorarId = null)
    {
        var sql = @"
            SELECT COUNT(1)
            FROM clientes c
            WHERE c.cpf_cnpj = @CpfCnpj AND c.nacionalidade_id = @NacionalidadeId";

        if (ignorarId.HasValue)
        {
            sql += " AND c.id != @IgnorarId";
        }

        var count = await _session.Connection.ExecuteScalarAsync<int>(
            sql,
            new { CpfCnpj = cpfCnpj, NacionalidadeId = nacionalidadeId, IgnorarId = ignorarId },
            transaction: _session.Transaction);

        return count > 0;
    }
}
