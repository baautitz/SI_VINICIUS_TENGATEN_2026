using Backend.Core.Common;
using Backend.Core.Features.Financeiro.DTOs;
using Backend.Core.Features.Financeiro.Entities;
using Backend.Core.Features.Financeiro.Repositories;
using Backend.Core.Features.Parceiros.Entities;
using Backend.Infrastructure.PostgreSQL.Common;
using Dapper;

namespace Backend.Infrastructure.PostgreSQL.Features.Financeiro;

public class ContasReceberRepository : IContasReceberRepository
{
    private readonly DbSession _session;

    public ContasReceberRepository(DbSession session)
    {
        _session = session;
    }

    public async Task<ResultadoPaginado<ContasReceber>> ObterContasReceber(int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string countSql = "SELECT COUNT(*) FROM contas_receber;";

        const string querySql = @"
            SELECT cr.id, cr.descricao, cr.data_emissao, cr.data_vencimento, cr.valor_original,
                   cr.valor_saldo, cr.status, cr.observacao, cr.criado_em, cr.atualizado_em,
                   c.id, c.nome_razao_social, c.cpf_cnpj, c.rg_ie, c.apelido_nome_fantasia,
                   c.endereco, c.telefone, c.email, c.limite_credito, c.ativo, c.criado_em,
                   c.atualizado_em, c.observacao
            FROM contas_receber cr
            JOIN clientes c ON c.id = cr.cliente_id
            ORDER BY cr.data_vencimento
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        var total = await _session.Connection.ExecuteScalarAsync<int>(
            countSql, transaction: _session.Transaction);

        var contas = (await _session.Connection.QueryAsync<ContasReceber, Clientes, ContasReceber>(
            querySql,
            (conta, cliente) =>
            {
                conta.Cliente = cliente;
                conta.ContasReceberParcelas = [];
                return conta;
            },
            new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction,
            splitOn: "id")).ToList();

        if (contas.Count > 0)
        {
            var ids = contas.Select(c => c.Id).ToArray();

            const string parcelasSql = @"
                SELECT id, conta_receber_id, numero_parcela, data_vencimento,
                       valor_parcela, valor_recebido, status
                FROM contas_receber_parcelas
                WHERE conta_receber_id = ANY(@Ids)
                ORDER BY conta_receber_id, numero_parcela;";

            var parcelas = (await _session.Connection.QueryAsync<ContasReceberParcelas>(
                parcelasSql, new { Ids = ids }, transaction: _session.Transaction)).ToList();

            var parcelasPorConta = parcelas
                .GroupBy(p => p.ContaReceberId)
                .ToDictionary(g => g.Key, g => g.AsEnumerable());

            foreach (var conta in contas)
            {
                if (parcelasPorConta.TryGetValue(conta.Id, out var sub))
                    conta.ContasReceberParcelas = sub;
            }
        }

        return new ResultadoPaginado<ContasReceber>(contas, total, pagina, tamanhoDaPagina);
    }

    public async Task<ContasReceber?> ObterContaReceberPorId(int id)
    {
        const string contaSql = @"
            SELECT cr.id, cr.descricao, cr.data_emissao, cr.data_vencimento, cr.valor_original,
                   cr.valor_saldo, cr.status, cr.observacao, cr.criado_em, cr.atualizado_em,
                   c.id, c.nome_razao_social, c.cpf_cnpj, c.rg_ie, c.apelido_nome_fantasia,
                   c.endereco, c.telefone, c.email, c.limite_credito, c.ativo, c.criado_em,
                   c.atualizado_em, c.observacao
            FROM contas_receber cr
            JOIN clientes c ON c.id = cr.cliente_id
            WHERE cr.id = @Id;";

        const string parcelasSql = @"
            SELECT id, conta_receber_id, numero_parcela, data_vencimento,
                   valor_parcela, valor_recebido, status
            FROM contas_receber_parcelas
            WHERE conta_receber_id = @Id
            ORDER BY numero_parcela;";

        var conta = (await _session.Connection.QueryAsync<ContasReceber, Clientes, ContasReceber>(
            contaSql,
            (c, cliente) =>
            {
                c.Cliente = cliente;
                c.ContasReceberParcelas = [];
                return c;
            },
            new { Id = id },
            transaction: _session.Transaction,
            splitOn: "id")).SingleOrDefault();

        if (conta is null) return null;

        conta.ContasReceberParcelas = await _session.Connection.QueryAsync<ContasReceberParcelas>(
            parcelasSql, new { Id = id }, transaction: _session.Transaction);

        return conta;
    }

    public async Task<ContasReceber> CriarContaReceber(ContasReceber conta)
    {
        const string sql = @"
            INSERT INTO contas_receber (descricao, data_emissao, data_vencimento, valor_original, valor_saldo,
                                       status, observacao, criado_em, cliente_id, nfe_id, condicao_pagamento_id)
            VALUES (@Descricao, @DataEmissao, @DataVencimento, @ValorOriginal, @ValorSaldo,
                    @Status, @Observacao, @CriadoEm, @ClienteId, @NfeId, @CondicaoPagamentoId)
            RETURNING id;";

        var idGerado = await _session.Connection.ExecuteScalarAsync<int>(
            sql,
            new
            {
                conta.Descricao, conta.DataEmissao, conta.DataVencimento,
                conta.ValorOriginal, conta.ValorSaldo, conta.Status, conta.Observacao,
                CriadoEm = DateTime.UtcNow,
                ClienteId = conta.Cliente.Id,
                NfeId = conta.Nfe?.Id,
                CondicaoPagamentoId = conta.CondicaoPagamento?.Id
            },
            transaction: _session.Transaction);

        conta.Id = idGerado;
        await InserirParcelas(idGerado, conta.ContasReceberParcelas);

        return conta;
    }

    public async Task<ContasReceber> AtualizarContaReceber(int id, ContasReceber conta)
    {
        const string sql = @"
            UPDATE contas_receber
            SET descricao = @Descricao, data_emissao = @DataEmissao, data_vencimento = @DataVencimento,
                valor_original = @ValorOriginal, valor_saldo = @ValorSaldo, status = @Status,
                observacao = @Observacao, atualizado_em = @AtualizadoEm,
                cliente_id = @ClienteId, nfe_id = @NfeId, condicao_pagamento_id = @CondicaoPagamentoId
            WHERE id = @Id;";

        await _session.Connection.ExecuteAsync(
            sql,
            new
            {
                Id = id, conta.Descricao, conta.DataEmissao, conta.DataVencimento,
                conta.ValorOriginal, conta.ValorSaldo, conta.Status, conta.Observacao,
                AtualizadoEm = DateTime.UtcNow,
                ClienteId = conta.Cliente.Id,
                NfeId = conta.Nfe?.Id,
                CondicaoPagamentoId = conta.CondicaoPagamento?.Id
            },
            transaction: _session.Transaction);

        await ReplacerParcelas(id, conta.ContasReceberParcelas);

        conta.Id = id;
        return conta;
    }

    public async Task<bool> DeletarContaReceber(int id)
    {
        await _session.Connection.ExecuteAsync(
            "DELETE FROM contas_receber_parcelas WHERE conta_receber_id = @Id;",
            new { Id = id }, transaction: _session.Transaction);

        var linhasAfetadas = await _session.Connection.ExecuteAsync(
            "DELETE FROM contas_receber WHERE id = @Id;",
            new { Id = id }, transaction: _session.Transaction);

        return linhasAfetadas > 0;
    }

    public async Task<ResultadoPaginado<ContasReceberResumo>> ObterContasReceberResumo(int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sql = @"
            SELECT COUNT(*) FROM contas_receber;

            SELECT cr.id, c.nome_razao_social AS cliente_nome, cr.descricao,
                   cr.data_vencimento, cr.valor_saldo, cr.status
            FROM contas_receber cr
            JOIN clientes c ON c.id = cr.cliente_id
            ORDER BY cr.data_vencimento
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql, new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction);

        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<ContasReceberResumo>();

        return new ResultadoPaginado<ContasReceberResumo>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<ResultadoPaginado<ContasReceberResumo>> PesquisarContasReceber(string termo, int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sql = @"
            SELECT COUNT(*)
            FROM contas_receber cr
            JOIN clientes c ON c.id = cr.cliente_id
            WHERE cr.descricao ILIKE @Termo OR c.nome_razao_social ILIKE @Termo;

            SELECT cr.id, c.nome_razao_social AS cliente_nome, cr.descricao,
                   cr.data_vencimento, cr.valor_saldo, cr.status
            FROM contas_receber cr
            JOIN clientes c ON c.id = cr.cliente_id
            WHERE cr.descricao ILIKE @Termo OR c.nome_razao_social ILIKE @Termo
            ORDER BY cr.data_vencimento
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql,
            new { Termo = $"%{termo}%", TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction);

        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<ContasReceberResumo>();

        return new ResultadoPaginado<ContasReceberResumo>(itens, total, pagina, tamanhoDaPagina);
    }

    private async Task InserirParcelas(int contaId, IEnumerable<ContasReceberParcelas> parcelas)
    {
        const string sql = @"
            INSERT INTO contas_receber_parcelas (numero_parcela, data_vencimento, valor_parcela, valor_recebido, status, conta_receber_id)
            VALUES (@NumeroParcela, @DataVencimento, @ValorParcela, @ValorRecebido, @Status, @ContaId);";

        await _session.Connection.ExecuteAsync(
            sql,
            parcelas.Select(p => new
            {
                p.NumeroParcela, p.DataVencimento, p.ValorParcela, p.ValorRecebido, p.Status,
                ContaId = contaId
            }),
            transaction: _session.Transaction);
    }

    private async Task ReplacerParcelas(int contaId, IEnumerable<ContasReceberParcelas> parcelas)
    {
        await _session.Connection.ExecuteAsync(
            "DELETE FROM contas_receber_parcelas WHERE conta_receber_id = @ContaId;",
            new { ContaId = contaId }, transaction: _session.Transaction);

        await InserirParcelas(contaId, parcelas);
    }
}
