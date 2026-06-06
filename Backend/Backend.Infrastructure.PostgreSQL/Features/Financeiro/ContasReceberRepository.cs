using System.Linq;
using Backend.Core.Common.Results;
using Backend.Core.Common.Enums;
using Backend.Core.Features.Localizacao.Entities;
using Backend.Core.Features.Financeiro.DTOs;
using Backend.Core.Features.Financeiro.Entities;
using Backend.Core.Features.Financeiro.Entities.Enums;
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
            SELECT cr.id AS Id, cr.descricao AS Descricao, cr.data_emissao AS DataEmissao, cr.data_vencimento AS DataVencimento,
                   cr.valor_original AS ValorOriginal, cr.valor_saldo AS ValorSaldo, cr.status AS Status, cr.observacao AS Observacao, cr.criado_em AS CriadoEm,
                   c.id AS ClienteId, c.tipo_pessoa AS ClienteTipoPessoa, c.nome_razaosocial AS ClienteNomeRazaoSocial, c.cpf_cnpj AS ClienteCpfCnpj,
                   c.rg_ie AS ClienteRgIe, c.apelido_nomefantasia AS ClienteApelidoNomeFantasia, c.endereco AS ClienteEndereco,
                   c.telefone AS ClienteTelefone, c.email AS ClienteEmail, c.limite_credito AS ClienteLimiteCredito,
                   c.ativo AS ClienteAtivo, c.criado_em AS ClienteCriadoEm, c.observacao AS ClienteObservacao,
                   p.id AS PaisId, p.ddi AS PaisDdi, p.sigla_iso AS PaisSiglaIso, p.moeda AS PaisMoeda, p.simbolo_moeda AS PaisSimboloMoeda, p.pais AS PaisNome
            FROM contas_receber cr
            JOIN clientes c ON c.id = cr.cliente_id
            JOIN paises p ON p.id = c.nacionalidade_id
            ORDER BY cr.data_vencimento
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        var total = await _session.Connection.ExecuteScalarAsync<int>(
            countSql, transaction: _session.Transaction);

        var contasDto = (await _session.Connection.QueryAsync<ContaReceberDto>(
            querySql,
            new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction)).ToList();

        var contas = new List<ContasReceber>();
        if (contasDto.Count > 0)
        {
            var ids = contasDto.Select(c => c.Id).ToArray();

            const string parcelasSql = @"
                SELECT id, conta_receber_id AS ContaReceberId, numero_parcela AS NumeroParcela, data_vencimento AS DataVencimento,
                       valor_parcela AS ValorParcela, valor_recebido AS ValorRecebido, status AS Status
                FROM contas_receber_parcelas
                WHERE conta_receber_id = ANY(@Ids)
                ORDER BY conta_receber_id, numero_parcela;";

            var parcelas = (await _session.Connection.QueryAsync<ParcelaReceberDto>(
                parcelasSql, new { Ids = ids }, transaction: _session.Transaction)).ToList();

            var parcelasPorConta = parcelas
                .GroupBy(p => p.ContaReceberId)
                .ToDictionary(g => g.Key, g => g.AsEnumerable());

            foreach (var dto in contasDto)
            {
                var conta = BuildContaReceber(dto);
                if (parcelasPorConta.TryGetValue(dto.Id, out var sub))
                {
                    foreach (var parcelaDto in sub)
                    {
                        conta.AdicionarParcelaExistente(BuildParcelaReceber(parcelaDto));
                    }
                }

                contas.Add(conta);
            }
        }

        return new ResultadoPaginado<ContasReceber>(contas, total, pagina, tamanhoDaPagina);
    }

    public async Task<ContasReceber?> ObterContaReceberPorId(int id)
    {
        const string contaSql = @"
            SELECT cr.id AS Id, cr.descricao AS Descricao, cr.data_emissao AS DataEmissao, cr.data_vencimento AS DataVencimento,
                   cr.valor_original AS ValorOriginal, cr.valor_saldo AS ValorSaldo, cr.status AS Status, cr.observacao AS Observacao, cr.criado_em AS CriadoEm,
                   c.id AS ClienteId, c.tipo_pessoa AS ClienteTipoPessoa, c.nome_razaosocial AS ClienteNomeRazaoSocial, c.cpf_cnpj AS ClienteCpfCnpj,
                   c.rg_ie AS ClienteRgIe, c.apelido_nomefantasia AS ClienteApelidoNomeFantasia, c.endereco AS ClienteEndereco,
                   c.telefone AS ClienteTelefone, c.email AS ClienteEmail, c.limite_credito AS ClienteLimiteCredito,
                   c.ativo AS ClienteAtivo, c.criado_em AS ClienteCriadoEm, c.observacao AS ClienteObservacao,
                   p.id AS PaisId, p.ddi AS PaisDdi, p.sigla_iso AS PaisSiglaIso, p.moeda AS PaisMoeda, p.simbolo_moeda AS PaisSimboloMoeda, p.pais AS PaisNome
            FROM contas_receber cr
            JOIN clientes c ON c.id = cr.cliente_id
            JOIN paises p ON p.id = c.nacionalidade_id
            WHERE cr.id = @Id;";

        const string parcelasSql = @"
            SELECT id, conta_receber_id AS ContaReceberId, numero_parcela AS NumeroParcela, data_vencimento AS DataVencimento,
                   valor_parcela AS ValorParcela, valor_recebido AS ValorRecebido, status AS Status
            FROM contas_receber_parcelas
            WHERE conta_receber_id = @Id
            ORDER BY numero_parcela;";

        var dto = await _session.Connection.QuerySingleOrDefaultAsync<ContaReceberDto>(
            contaSql,
            new { Id = id },
            transaction: _session.Transaction);

        if (dto is null) return null;

        var conta = BuildContaReceber(dto);
        var parcelas = await _session.Connection.QueryAsync<ParcelaReceberDto>(
            parcelasSql, new { Id = id }, transaction: _session.Transaction);

        foreach (var parcelaDto in parcelas)
        {
            conta.AdicionarParcelaExistente(BuildParcelaReceber(parcelaDto));
        }

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
                conta.Descricao,
                conta.DataEmissao,
                conta.DataVencimento,
                conta.ValorOriginal,
                conta.ValorSaldo,
                conta.Status,
                conta.Observacao,
                CriadoEm = DateTime.UtcNow,
                ClienteId = conta.Cliente.Id,
                NfeId = conta.Nfe?.Id,
                CondicaoPagamentoId = conta.CondicaoPagamento?.Id
            },
            transaction: _session.Transaction);

        await InserirParcelas(idGerado, conta.ContasReceberParcelas);

        var created = new ContasReceber(idGerado, conta.Descricao, conta.ValorOriginal, conta.Cliente, conta.DataEmissao, conta.DataVencimento, conta.CondicaoPagamento, conta.Nfe, conta.Observacao, conta.CriadoEm, conta.Status);
        foreach (var parcela in conta.ContasReceberParcelas)
        {
            created.AdicionarParcelaExistente(new ContasReceberParcelas(parcela.Id, idGerado, parcela.NumeroParcela, parcela.DataVencimento, parcela.ValorParcela, parcela.ValorRecebido, parcela.Status));
        }

        return created;
    }

    public async Task<ContasReceber> AtualizarContaReceber(int id, ContasReceber conta)
    {
        const string sql = @"
            UPDATE contas_receber
            SET descricao = @Descricao, data_emissao = @DataEmissao, data_vencimento = @DataVencimento,
                valor_original = @ValorOriginal, valor_saldo = @ValorSaldo, status = @Status,
                observacao = @Observacao,
                cliente_id = @ClienteId, nfe_id = @NfeId, condicao_pagamento_id = @CondicaoPagamentoId
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
                ClienteId = conta.Cliente.Id,
                NfeId = conta.Nfe?.Id,
                CondicaoPagamentoId = conta.CondicaoPagamento?.Id
            },
            transaction: _session.Transaction);

        await ReplacerParcelas(id, conta.ContasReceberParcelas);

        var updated = new ContasReceber(id, conta.Descricao, conta.ValorOriginal, conta.Cliente, conta.DataEmissao, conta.DataVencimento, conta.CondicaoPagamento, conta.Nfe, conta.Observacao, conta.CriadoEm, conta.Status);
        foreach (var parcela in conta.ContasReceberParcelas)
        {
            updated.AdicionarParcelaExistente(new ContasReceberParcelas(parcela.Id, id, parcela.NumeroParcela, parcela.DataVencimento, parcela.ValorParcela, parcela.ValorRecebido, parcela.Status));
        }

        return updated;
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

            SELECT cr.id, c.nome_razaosocial AS cliente_nome, cr.descricao,
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
            WHERE cr.descricao ILIKE @Termo OR c.nome_razaosocial ILIKE @Termo;

            SELECT cr.id, c.nome_razaosocial AS cliente_nome, cr.descricao,
                   cr.data_vencimento, cr.valor_saldo, cr.status
            FROM contas_receber cr
            JOIN clientes c ON c.id = cr.cliente_id
            WHERE cr.descricao ILIKE @Termo OR c.nome_razaosocial ILIKE @Termo
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
                p.NumeroParcela,
                p.DataVencimento,
                p.ValorParcela,
                p.ValorRecebido,
                p.Status,
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

    private static ContasReceber BuildContaReceber(ContaReceberDto dto)
    {
        var pais = new Paises(dto.PaisId, dto.PaisDdi, dto.PaisSiglaIso, dto.PaisMoeda, dto.PaisSimboloMoeda, dto.PaisNome);
        var tipoPessoa = Enum.Parse<TipoPessoa>(dto.ClienteTipoPessoa);

        var cliente = new Clientes(
            dto.ClienteId,
            tipoPessoa,
            dto.ClienteNomeRazaoSocial,
            dto.ClienteCpfCnpj,
            pais,
            dto.ClienteRgIe,
            dto.ClienteApelidoNomeFantasia,
            dto.ClienteEndereco,
            null,
            dto.ClienteTelefone,
            dto.ClienteEmail,
            dto.ClienteLimiteCredito,
            dto.ClienteObservacao,
            dto.ClienteAtivo,
            dto.ClienteCriadoEm);

        return new ContasReceber(
            dto.Id,
            dto.Descricao,
            dto.ValorOriginal,
            cliente,
            dto.DataEmissao,
            dto.DataVencimento,
            null,
            null,
            dto.Observacao,
            dto.CriadoEm,
            dto.Status);
    }

    private static ContasReceberParcelas BuildParcelaReceber(ParcelaReceberDto dto)
    {
        return new ContasReceberParcelas(dto.Id, dto.ContaReceberId, dto.NumeroParcela, dto.DataVencimento, dto.ValorParcela, dto.ValorRecebido, dto.Status);
    }

    private sealed record ContaReceberDto(
        int Id,
        string Descricao,
        DateTime? DataEmissao,
        DateTime? DataVencimento,
        decimal ValorOriginal,
        decimal ValorSaldo,
        StatusTituloFinanceiro Status,
        string? Observacao,
        DateTime CriadoEm,
        int ClienteId,
        string ClienteTipoPessoa,
        string ClienteNomeRazaoSocial,
        string ClienteCpfCnpj,
        string? ClienteRgIe,
        string? ClienteApelidoNomeFantasia,
        string? ClienteEndereco,
        string? ClienteTelefone,
        string? ClienteEmail,
        decimal ClienteLimiteCredito,
        bool ClienteAtivo,
        DateTime ClienteCriadoEm,
        string? ClienteObservacao,
        int PaisId,
        string PaisDdi,
        string PaisSiglaIso,
        string PaisMoeda,
        string PaisSimboloMoeda,
        string PaisNome);

    private sealed record ParcelaReceberDto(
        int Id,
        int ContaReceberId,
        int NumeroParcela,
        DateTime DataVencimento,
        decimal ValorParcela,
        decimal ValorRecebido,
        StatusTituloFinanceiro Status);
}
