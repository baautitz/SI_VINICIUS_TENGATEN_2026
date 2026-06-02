using System.Linq;
using System.Linq;
using Backend.Core.Common.Results;
using Backend.Core.Features.Financeiro.DTOs;
using Backend.Core.Features.Financeiro.Entities;
using Backend.Core.Features.Financeiro.Entities.Enums;
using Backend.Core.Features.Financeiro.Repositories;
using Backend.Core.Features.Parceiros.Entities;
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
            SELECT cp.id AS Id, cp.descricao AS Descricao, cp.data_emissao AS DataEmissao, cp.data_vencimento AS DataVencimento, cp.valor_original AS ValorOriginal,
                   cp.valor_saldo AS ValorSaldo, cp.status AS Status, cp.observacao AS Observacao, cp.criado_em AS CriadoEm,
                   f.id AS FornecedorId, f.nome_razaosocial AS FornecedorNomeRazaosocial, f.cpf_cnpj AS FornecedorCpfCnpj,
                   f.rg_ie AS FornecedorRgIe, f.apelido_nomefantasia AS FornecedorApelidoNomeFantasia, f.endereco AS FornecedorEndereco,
                   f.telefone AS FornecedorTelefone, f.email AS FornecedorEmail, f.ativo AS FornecedorAtivo, f.criado_em AS FornecedorCriadoEm, f.observacao AS FornecedorObservacao
            FROM contas_pagar cp
            JOIN fornecedores f ON f.id = cp.fornecedor_id
            ORDER BY cp.data_vencimento
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        var total = await _session.Connection.ExecuteScalarAsync<int>(
            countSql, transaction: _session.Transaction);

        var contasDto = (await _session.Connection.QueryAsync<ContaPagarDto>(
            querySql,
            new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction)).ToList();

        var contas = new List<ContasPagar>();
        if (contasDto.Count > 0)
        {
            var ids = contasDto.Select(c => c.Id).ToArray();

            const string parcelasSql = @"
                SELECT id, conta_pagar_id AS ContaPagarId, numero_parcela AS NumeroParcela, data_vencimento AS DataVencimento,
                       valor_parcela AS ValorParcela, valor_pago AS ValorPago, status AS Status
                FROM contas_pagar_parcelas
                WHERE conta_pagar_id = ANY(@Ids)
                ORDER BY conta_pagar_id, numero_parcela;";

            var parcelas = (await _session.Connection.QueryAsync<ParcelaPagarDto>(
                parcelasSql, new { Ids = ids }, transaction: _session.Transaction)).ToList();

            var parcelasPorConta = parcelas
                .GroupBy(p => p.ContaPagarId)
                .ToDictionary(g => g.Key, g => g.AsEnumerable());

            foreach (var dto in contasDto)
            {
                var conta = BuildContaPagar(dto);
                if (parcelasPorConta.TryGetValue(dto.Id, out var sub))
                {
                    foreach (var parcelaDto in sub)
                    {
                        conta.AdicionarParcelaExistente(BuildParcelaPagar(parcelaDto));
                    }
                }

                contas.Add(conta);
            }
        }

        return new ResultadoPaginado<ContasPagar>(contas, total, pagina, tamanhoDaPagina);
    }

    public async Task<ContasPagar?> ObterContaPagarPorId(int id)
    {
        const string contaSql = @"
            SELECT cp.id AS Id, cp.descricao AS Descricao, cp.data_emissao AS DataEmissao, cp.data_vencimento AS DataVencimento, cp.valor_original AS ValorOriginal,
                   cp.valor_saldo AS ValorSaldo, cp.status AS Status, cp.observacao AS Observacao, cp.criado_em AS CriadoEm,
                   f.id AS FornecedorId, f.nome_razaosocial AS FornecedorNomeRazaosocial, f.cpf_cnpj AS FornecedorCpfCnpj,
                   f.rg_ie AS FornecedorRgIe, f.apelido_nomefantasia AS FornecedorApelidoNomeFantasia, f.endereco AS FornecedorEndereco,
                   f.telefone AS FornecedorTelefone, f.email AS FornecedorEmail, f.ativo AS FornecedorAtivo, f.criado_em AS FornecedorCriadoEm, f.observacao AS FornecedorObservacao
            FROM contas_pagar cp
            JOIN fornecedores f ON f.id = cp.fornecedor_id
            WHERE cp.id = @Id;";

        const string parcelasSql = @"
            SELECT id, conta_pagar_id AS ContaPagarId, numero_parcela AS NumeroParcela, data_vencimento AS DataVencimento,
                   valor_parcela AS ValorParcela, valor_pago AS ValorPago, status AS Status
            FROM contas_pagar_parcelas
            WHERE conta_pagar_id = @Id
            ORDER BY numero_parcela;";

        var dto = await _session.Connection.QuerySingleOrDefaultAsync<ContaPagarDto>(
            contaSql,
            new { Id = id },
            transaction: _session.Transaction);

        if (dto is null) return null;

        var conta = BuildContaPagar(dto);
        var parcelas = await _session.Connection.QueryAsync<ParcelaPagarDto>(
            parcelasSql, new { Id = id }, transaction: _session.Transaction);

        foreach (var parcelaDto in parcelas)
        {
            conta.AdicionarParcelaExistente(BuildParcelaPagar(parcelaDto));
        }

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
                conta.Descricao,
                conta.DataEmissao,
                conta.DataVencimento,
                conta.ValorOriginal,
                conta.ValorSaldo,
                conta.Status,
                conta.Observacao,
                CriadoEm = DateTime.UtcNow,
                FornecedorId = conta.Fornecedor.Id,
                NfeId = conta.Nfe?.Id,
                CondicaoPagamentoId = conta.CondicaoPagamento?.Id
            },
            transaction: _session.Transaction);

        await InserirParcelas(idGerado, conta.ContasPagarParcelas);

        var created = new ContasPagar(idGerado, conta.Descricao, conta.ValorOriginal, conta.Fornecedor, conta.DataEmissao, conta.DataVencimento, conta.CondicaoPagamento, conta.Nfe, conta.Observacao, conta.CriadoEm, conta.Status);
        foreach (var parcela in conta.ContasPagarParcelas)
        {
            created.AdicionarParcelaExistente(new ContasPagarParcelas(parcela.Id, idGerado, parcela.NumeroParcela, parcela.DataVencimento, parcela.ValorParcela, parcela.ValorPago, parcela.Status));
        }

        return created;
    }

    public async Task<ContasPagar> AtualizarContaPagar(int id, ContasPagar conta)
    {
        const string sql = @"
            UPDATE contas_pagar
            SET descricao = @Descricao, data_emissao = @DataEmissao, data_vencimento = @DataVencimento,
                valor_original = @ValorOriginal, valor_saldo = @ValorSaldo, status = @Status,
                observacao = @Observacao,
                fornecedor_id = @FornecedorId, nfe_id = @NfeId, condicao_pagamento_id = @CondicaoPagamentoId
            WHERE id = @Id;";

        await _session.Connection.ExecuteAsync(
            sql,
            new
            {
                Id = id,
                conta.Descricao,
                conta.DataEmissao,
                conta.DataVencimento,
                conta.ValorOriginal,
                conta.ValorSaldo,
                conta.Status,
                conta.Observacao,
                FornecedorId = conta.Fornecedor.Id,
                NfeId = conta.Nfe?.Id,
                CondicaoPagamentoId = conta.CondicaoPagamento?.Id
            },
            transaction: _session.Transaction);

        await ReplacerParcelas(id, conta.ContasPagarParcelas);

        var updated = new ContasPagar(id, conta.Descricao, conta.ValorOriginal, conta.Fornecedor, conta.DataEmissao, conta.DataVencimento, conta.CondicaoPagamento, conta.Nfe, conta.Observacao, conta.CriadoEm, conta.Status);
        foreach (var parcela in conta.ContasPagarParcelas)
        {
            updated.AdicionarParcelaExistente(new ContasPagarParcelas(parcela.Id, id, parcela.NumeroParcela, parcela.DataVencimento, parcela.ValorParcela, parcela.ValorPago, parcela.Status));
        }

        return updated;
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
                p.NumeroParcela,
                p.DataVencimento,
                p.ValorParcela,
                p.ValorPago,
                p.Status,
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

    private static ContasPagar BuildContaPagar(ContaPagarDto dto)
    {
        var fornecedor = new Fornecedores(
            dto.FornecedorId,
            dto.FornecedorNomeRazaosocial,
            dto.FornecedorCpfCnpj,
            dto.FornecedorRgIe,
            dto.FornecedorApelidoNomeFantasia,
            dto.FornecedorEndereco,
            null,
            dto.FornecedorTelefone,
            dto.FornecedorEmail,
            dto.FornecedorObservacao,
            dto.FornecedorAtivo,
            dto.FornecedorCriadoEm);

        return new ContasPagar(
            dto.Id,
            dto.Descricao,
            dto.ValorOriginal,
            fornecedor,
            dto.DataEmissao,
            dto.DataVencimento,
            null,
            null,
            dto.Observacao,
            dto.CriadoEm,
            dto.Status);
    }

    private static ContasPagarParcelas BuildParcelaPagar(ParcelaPagarDto dto)
    {
        return new ContasPagarParcelas(dto.Id, dto.ContaPagarId, dto.NumeroParcela, dto.DataVencimento, dto.ValorParcela, dto.ValorPago, dto.Status);
    }

    private sealed record ContaPagarDto(
        int Id,
        string Descricao,
        DateTime? DataEmissao,
        DateTime? DataVencimento,
        decimal ValorOriginal,
        decimal ValorSaldo,
        StatusTituloFinanceiro Status,
        string? Observacao,
        DateTime CriadoEm,
        int FornecedorId,
        string FornecedorNomeRazaosocial,
        string FornecedorCpfCnpj,
        string? FornecedorRgIe,
        string? FornecedorApelidoNomeFantasia,
        string? FornecedorEndereco,
        string? FornecedorTelefone,
        string? FornecedorEmail,
        bool FornecedorAtivo,
        DateTime FornecedorCriadoEm,
        string? FornecedorObservacao);

    private sealed record ParcelaPagarDto(
        int Id,
        int ContaPagarId,
        int NumeroParcela,
        DateTime DataVencimento,
        decimal ValorParcela,
        decimal ValorPago,
        StatusTituloFinanceiro Status);
}
