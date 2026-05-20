using Backend.Core.Common;
using Backend.Core.Features.Financeiro.DTOs;
using Backend.Core.Features.Financeiro.Entities;
using Backend.Core.Features.Financeiro.Repositories;
using Backend.Core.Features.Logistica.Entities;
using Backend.Infrastructure.PostgreSQL.Common;
using Dapper;

namespace Backend.Infrastructure.PostgreSQL.Features.Financeiro;

public class ContasPagarRepository : IContasPagarRepository
{
    private readonly DbSession _session;

    public ContasPagarRepository(DbSession session)
    {
        _session = session;
    }

    public async Task<ResultadoPaginado<ContasPagar>> ObterContasPagar(int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string countSql = "SELECT COUNT(*) FROM contas_pagar;";

        const string querySql = @"
            SELECT cp.id, cp.descricao, cp.data_emissao, cp.data_vencimento, cp.valor_original,
                   cp.valor_saldo, cp.status, cp.observacao, cp.criado_em, cp.atualizado_em,
                   f.id, f.nome_razaosocial, f.cpf_cnpj, f.rg_ie, f.apelido_nomefantasia,
                   f.endereco, f.telefone, f.email, f.ativo, f.criado_em, f.atualizado_em, f.observacao
            FROM contas_pagar cp
            JOIN fornecedores f ON f.id = cp.fornecedor_id
            ORDER BY cp.data_vencimento
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        var total = await _session.Connection.ExecuteScalarAsync<int>(
            countSql, transaction: _session.Transaction);

        var contas = (await _session.Connection.QueryAsync<ContasPagar, Fornecedores, ContasPagar>(
            querySql,
            (conta, fornecedor) =>
            {
                conta.Fornecedor = fornecedor;
                conta.ContasPagarParcelas = [];
                return conta;
            },
            new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction,
            splitOn: "id")).ToList();

        if (contas.Count > 0)
        {
            var ids = contas.Select(c => c.Id).ToArray();

            const string parcelasSql = @"
                SELECT id, conta_pagar_id, numero_parcela, data_vencimento, valor_parcela, valor_pago, status
                FROM contas_pagar_parcelas
                WHERE conta_pagar_id = ANY(@Ids)
                ORDER BY conta_pagar_id, numero_parcela;";

            var parcelas = (await _session.Connection.QueryAsync<ContasPagarParcelas>(
                parcelasSql, new { Ids = ids }, transaction: _session.Transaction)).ToList();

            var parcelasPorConta = parcelas
                .GroupBy(p => p.ContaPagarId)
                .ToDictionary(g => g.Key, g => g.AsEnumerable());

            foreach (var conta in contas)
            {
                if (parcelasPorConta.TryGetValue(conta.Id, out var sub))
                    conta.ContasPagarParcelas = sub;
            }
        }

        return new ResultadoPaginado<ContasPagar>(contas, total, pagina, tamanhoDaPagina);
    }

    public async Task<ContasPagar?> ObterContaPagarPorId(int id)
    {
        const string contaSql = @"
            SELECT cp.id, cp.descricao, cp.data_emissao, cp.data_vencimento, cp.valor_original,
                   cp.valor_saldo, cp.status, cp.observacao, cp.criado_em, cp.atualizado_em,
                   f.id, f.nome_razaosocial, f.cpf_cnpj, f.rg_ie, f.apelido_nomefantasia,
                   f.endereco, f.telefone, f.email, f.ativo, f.criado_em, f.atualizado_em, f.observacao
            FROM contas_pagar cp
            JOIN fornecedores f ON f.id = cp.fornecedor_id
            WHERE cp.id = @Id;";

        const string parcelasSql = @"
            SELECT id, conta_pagar_id, numero_parcela, data_vencimento, valor_parcela, valor_pago, status
            FROM contas_pagar_parcelas
            WHERE conta_pagar_id = @Id
            ORDER BY numero_parcela;";

        var conta = (await _session.Connection.QueryAsync<ContasPagar, Fornecedores, ContasPagar>(
            contaSql,
            (c, fornecedor) =>
            {
                c.Fornecedor = fornecedor;
                c.ContasPagarParcelas = [];
                return c;
            },
            new { Id = id },
            transaction: _session.Transaction,
            splitOn: "id")).SingleOrDefault();

        if (conta is null) return null;

        conta.ContasPagarParcelas = await _session.Connection.QueryAsync<ContasPagarParcelas>(
            parcelasSql, new { Id = id }, transaction: _session.Transaction);

        return conta;
    }

    public async Task<ContasPagar> CriarContaPagar(ContasPagar conta)
    {
        const string sql = @"
            INSERT INTO contas_pagar (descricao, data_emissao, data_vencimento, valor_original, valor_saldo,
                                     status, observacao, criado_em, fornecedor_id, nfe_id, condicao_pagamento_id)
            VALUES (@Descricao, @DataEmissao, @DataVencimento, @ValorOriginal, @ValorSaldo,
                    @Status, @Observacao, @CriadoEm, @FornecedorId, @NfeId, @CondicaoPagamentoId)
            RETURNING id;";

        var idGerado = await _session.Connection.ExecuteScalarAsync<int>(
            sql,
            new
            {
                conta.Descricao, conta.DataEmissao, conta.DataVencimento,
                conta.ValorOriginal, conta.ValorSaldo, conta.Status, conta.Observacao,
                CriadoEm = DateTime.UtcNow,
                FornecedorId = conta.Fornecedor.Id,
                NfeId = conta.Nfe?.Id,
                CondicaoPagamentoId = conta.CondicaoPagamento?.Id
            },
            transaction: _session.Transaction);

        conta.Id = idGerado;
        await InserirParcelas(idGerado, conta.ContasPagarParcelas);

        return conta;
    }

    public async Task<ContasPagar> AtualizarContaPagar(int id, ContasPagar conta)
    {
        const string sql = @"
            UPDATE contas_pagar
            SET descricao = @Descricao, data_emissao = @DataEmissao, data_vencimento = @DataVencimento,
                valor_original = @ValorOriginal, valor_saldo = @ValorSaldo, status = @Status,
                observacao = @Observacao, atualizado_em = @AtualizadoEm,
                fornecedor_id = @FornecedorId, nfe_id = @NfeId, condicao_pagamento_id = @CondicaoPagamentoId
            WHERE id = @Id;";

        await _session.Connection.ExecuteAsync(
            sql,
            new
            {
                Id = id, conta.Descricao, conta.DataEmissao, conta.DataVencimento,
                conta.ValorOriginal, conta.ValorSaldo, conta.Status, conta.Observacao,
                AtualizadoEm = DateTime.UtcNow,
                FornecedorId = conta.Fornecedor.Id,
                NfeId = conta.Nfe?.Id,
                CondicaoPagamentoId = conta.CondicaoPagamento?.Id
            },
            transaction: _session.Transaction);

        await ReplacerParcelas(id, conta.ContasPagarParcelas);

        conta.Id = id;
        return conta;
    }

    public async Task<bool> DeletarContaPagar(int id)
    {
        await _session.Connection.ExecuteAsync(
            "DELETE FROM contas_pagar_parcelas WHERE conta_pagar_id = @Id;",
            new { Id = id }, transaction: _session.Transaction);

        var linhasAfetadas = await _session.Connection.ExecuteAsync(
            "DELETE FROM contas_pagar WHERE id = @Id;",
            new { Id = id }, transaction: _session.Transaction);

        return linhasAfetadas > 0;
    }

    public async Task<ResultadoPaginado<ContasPagarResumo>> ObterContasPagarResumo(int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sql = @"
            SELECT COUNT(*) FROM contas_pagar;

            SELECT cp.id, f.nome_razaosocial AS fornecedor_nome, cp.descricao,
                   cp.data_vencimento, cp.valor_saldo, cp.status
            FROM contas_pagar cp
            JOIN fornecedores f ON f.id = cp.fornecedor_id
            ORDER BY cp.data_vencimento
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql, new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction);

        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<ContasPagarResumo>();

        return new ResultadoPaginado<ContasPagarResumo>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<ResultadoPaginado<ContasPagarResumo>> PesquisarContasPagar(string termo, int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sql = @"
            SELECT COUNT(*)
            FROM contas_pagar cp
            JOIN fornecedores f ON f.id = cp.fornecedor_id
            WHERE cp.descricao ILIKE @Termo OR f.nome_razaosocial ILIKE @Termo;

            SELECT cp.id, f.nome_razaosocial AS fornecedor_nome, cp.descricao,
                   cp.data_vencimento, cp.valor_saldo, cp.status
            FROM contas_pagar cp
            JOIN fornecedores f ON f.id = cp.fornecedor_id
            WHERE cp.descricao ILIKE @Termo OR f.nome_razaosocial ILIKE @Termo
            ORDER BY cp.data_vencimento
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql,
            new { Termo = $"%{termo}%", TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction);

        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<ContasPagarResumo>();

        return new ResultadoPaginado<ContasPagarResumo>(itens, total, pagina, tamanhoDaPagina);
    }

    private async Task InserirParcelas(int contaId, IEnumerable<ContasPagarParcelas> parcelas)
    {
        const string sql = @"
            INSERT INTO contas_pagar_parcelas (numero_parcela, data_vencimento, valor_parcela, valor_pago, status, conta_pagar_id)
            VALUES (@NumeroParcela, @DataVencimento, @ValorParcela, @ValorPago, @Status, @ContaId);";

        await _session.Connection.ExecuteAsync(
            sql,
            parcelas.Select(p => new
            {
                p.NumeroParcela, p.DataVencimento, p.ValorParcela, p.ValorPago, p.Status,
                ContaId = contaId
            }),
            transaction: _session.Transaction);
    }

    private async Task ReplacerParcelas(int contaId, IEnumerable<ContasPagarParcelas> parcelas)
    {
        await _session.Connection.ExecuteAsync(
            "DELETE FROM contas_pagar_parcelas WHERE conta_pagar_id = @ContaId;",
            new { ContaId = contaId }, transaction: _session.Transaction);

        await InserirParcelas(contaId, parcelas);
    }
}
