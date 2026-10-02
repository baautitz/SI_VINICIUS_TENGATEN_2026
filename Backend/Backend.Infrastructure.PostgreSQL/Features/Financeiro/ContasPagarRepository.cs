using System.Linq;
using Backend.Core.Common.Results;
using Backend.Core.Features.Parceiros.Enums;
using Backend.Core.Common.ValueObjects;
using Backend.Core.Features.Localizacao.Entities;
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
                   cp.origem_tipo::text AS OrigemTipo, cp.origem_id AS OrigemId, cp.cliente_id AS ClienteId, cli.nome_razaosocial AS ClienteNome,
                   f.id AS Id, f.tipo_pessoa AS TipoPessoa, f.nome_razaosocial AS NomeRazaosocial, f.cpf_cnpj AS CpfCnpj,
                   f.rg_ie AS RgIe, f.apelido_nomefantasia AS ApelidoNomefantasia, f.logradouro AS Logradouro, f.numero AS Numero,
                   f.telefone AS Telefone, f.email AS Email, f.ativo AS Ativo, f.criado_em AS CriadoEm, f.observacao AS Observacao,
                   p.id AS Id, p.ddi AS Ddi, p.codigo_iso_pais AS CodigoIsoPais, p.codigo_iso_moeda AS CodigoIsoMoeda, p.simbolo_moeda AS SimboloMoeda, p.pais AS Pais,
                   con.id AS Id, con.descricao AS Descricao, con.entrada_minima_percentual AS EntradaMinimaPercentual,
                   con.desconto_percentual AS DescontoPercentual, con.acrescimo_percentual AS AcrescimoPercentual,
                   con.multa_percentual AS MultaPercentual, con.taxa_juros_percentual AS TaxaJurosPercentual, con.ativo AS Ativo,
                   mp.codigo AS Codigo, mp.descricao AS Descricao, mp.ativo AS Ativo
            FROM contas_pagar cp
            LEFT JOIN fornecedores f ON f.id = cp.fornecedor_id
            LEFT JOIN paises p ON p.id = f.nacionalidade_id
            LEFT JOIN clientes cli ON cli.id = cp.cliente_id
            LEFT JOIN condicoes_pagamentos con ON con.id = cp.condicao_pagamento_id
            LEFT JOIN metodos_pagamento mp ON mp.codigo = con.metodo_pagamento_codigo
            ORDER BY cp.criado_em DESC, cp.id DESC
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        var total = await _session.Connection.ExecuteScalarAsync<int>(
            countSql, transaction: _session.Transaction);

        var contas = (await _session.Connection.QueryAsync<ContasPagar, Fornecedores, Paises, CondicoesPagamentos, MetodosPagamentos, ContasPagar>(
            querySql,
            (conta, fornecedor, pais, condicao, metodo) =>
            {
                var f = fornecedor is null ? null : new Fornecedores(
                    fornecedor.Id,
                    fornecedor.TipoPessoa,
                    fornecedor.NomeRazaosocial,
                    fornecedor.CpfCnpj,
                    pais,
                    fornecedor.RgIe,
                    fornecedor.ApelidoNomefantasia,
                    fornecedor.Logradouro,
                    fornecedor.Numero,
                    null,
                    fornecedor.Telefone,
                    fornecedor.Email,
                    fornecedor.Observacao,
                    fornecedor.Ativo,
                    fornecedor.CriadoEm
                );

                if (condicao != null && metodo != null)
                {
                    condicao.AtualizarMetodoPagamento(metodo);
                }

                return new ContasPagar(
                    conta.Id,
                    conta.Descricao,
                    conta.ValorOriginal,
                    f,
                    conta.DataEmissao,
                    conta.DataVencimento,
                    condicao,
                    conta.OrigemTipo,
                    conta.OrigemId,
                    conta.Observacao,
                    conta.CriadoEm,
                    conta.Status,
                    conta.ClienteId,
                    conta.ClienteNome
                );
            },
            new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction,
            splitOn: "Id,Id,Id,Codigo"
        )).ToList();

        if (contas.Count > 0)
        {
            var ids = contas.Select(c => c.Id).ToArray();

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

            foreach (var conta in contas)
            {
                if (parcelasPorConta.TryGetValue(conta.Id, out var sub))
                {
                    foreach (var parcelaDto in sub)
                    {
                        conta.AdicionarParcelaExistente(BuildParcelaPagar(parcelaDto));
                    }
                }
            }
        }

        return new ResultadoPaginado<ContasPagar>(contas, total, pagina, tamanhoDaPagina);
    }

    public async Task<ContasPagar?> ObterContaPagarPorId(int id)
    {
        const string contaSql = @"
            SELECT cp.id AS Id, cp.descricao AS Descricao, cp.data_emissao AS DataEmissao, cp.data_vencimento AS DataVencimento, cp.valor_original AS ValorOriginal,
                   cp.valor_saldo AS ValorSaldo, cp.status AS Status, cp.observacao AS Observacao, cp.criado_em AS CriadoEm,
                   cp.origem_tipo::text AS OrigemTipo, cp.origem_id AS OrigemId, cp.cliente_id AS ClienteId, cli.nome_razaosocial AS ClienteNome,
                   f.id AS Id, f.tipo_pessoa AS TipoPessoa, f.nome_razaosocial AS NomeRazaosocial, f.cpf_cnpj AS CpfCnpj,
                   f.rg_ie AS RgIe, f.apelido_nomefantasia AS ApelidoNomefantasia, f.logradouro AS Logradouro, f.numero AS Numero,
                   f.telefone AS Telefone, f.email AS Email, f.ativo AS Ativo, f.criado_em AS CriadoEm, f.observacao AS Observacao,
                   p.id AS Id, p.ddi AS Ddi, p.codigo_iso_pais AS CodigoIsoPais, p.codigo_iso_moeda AS CodigoIsoMoeda, p.simbolo_moeda AS SimboloMoeda, p.pais AS Pais,
                   con.id AS Id, con.descricao AS Descricao, con.entrada_minima_percentual AS EntradaMinimaPercentual,
                   con.desconto_percentual AS DescontoPercentual, con.acrescimo_percentual AS AcrescimoPercentual,
                   con.multa_percentual AS MultaPercentual, con.taxa_juros_percentual AS TaxaJurosPercentual, con.ativo AS Ativo,
                   mp.codigo AS Codigo, mp.descricao AS Descricao, mp.ativo AS Ativo
            FROM contas_pagar cp
            LEFT JOIN fornecedores f ON f.id = cp.fornecedor_id
            LEFT JOIN paises p ON p.id = f.nacionalidade_id
            LEFT JOIN clientes cli ON cli.id = cp.cliente_id
            LEFT JOIN condicoes_pagamentos con ON con.id = cp.condicao_pagamento_id
            LEFT JOIN metodos_pagamento mp ON mp.codigo = con.metodo_pagamento_codigo
            WHERE cp.id = @Id;";

        const string parcelasSql = @"
            SELECT id, conta_pagar_id AS ContaPagarId, numero_parcela AS NumeroParcela, data_vencimento AS DataVencimento,
                   valor_parcela AS ValorParcela, valor_pago AS ValorPago, status AS Status
            FROM contas_pagar_parcelas
            WHERE conta_pagar_id = @Id
            ORDER BY numero_parcela;";

        var conta = (await _session.Connection.QueryAsync<ContasPagar, Fornecedores, Paises, CondicoesPagamentos, MetodosPagamentos, ContasPagar>(
            contaSql,
            (conta, fornecedor, pais, condicao, metodo) =>
            {
                var f = fornecedor is null ? null : new Fornecedores(
                    fornecedor.Id,
                    fornecedor.TipoPessoa,
                    fornecedor.NomeRazaosocial,
                    fornecedor.CpfCnpj,
                    pais,
                    fornecedor.RgIe,
                    fornecedor.ApelidoNomefantasia,
                    fornecedor.Logradouro,
                    fornecedor.Numero,
                    null,
                    fornecedor.Telefone,
                    fornecedor.Email,
                    fornecedor.Observacao,
                    fornecedor.Ativo,
                    fornecedor.CriadoEm
                );

                if (condicao != null && metodo != null)
                {
                    condicao.AtualizarMetodoPagamento(metodo);
                }

                return new ContasPagar(
                    conta.Id,
                    conta.Descricao,
                    conta.ValorOriginal,
                    f,
                    conta.DataEmissao,
                    conta.DataVencimento,
                    condicao,
                    conta.OrigemTipo,
                    conta.OrigemId,
                    conta.Observacao,
                    conta.CriadoEm,
                    conta.Status,
                    conta.ClienteId,
                    conta.ClienteNome
                );
            },
            new { Id = id },
            transaction: _session.Transaction,
            splitOn: "Id,Id,Id,Codigo"
        )).SingleOrDefault();

        if (conta is null) return null;

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
                                     status, observacao, criado_em, fornecedor_id, cliente_id, origem_tipo, condicao_pagamento_id, origem_id)
            VALUES (@Descricao, @DataEmissao, @DataVencimento, @ValorOriginal, @ValorSaldo,
                    @Status::status_titulo_financeiro_enum, @Observacao, @CriadoEm, @FornecedorId, @ClienteId, @OrigemTipo::origem_titulo_financeiro_enum, @CondicaoPagamentoId, @OrigemId)
            RETURNING id;";

        var idGerado = await _session.Connection.ExecuteScalarAsync<int>(
            sql,
            new ContaPagarDbRow(
                0,
                conta.Descricao,
                conta.DataEmissao,
                conta.DataVencimento,
                conta.ValorOriginal,
                conta.ValorSaldo,
                conta.Status.ToString(),
                conta.Observacao,
                DateTime.UtcNow,
                conta.Fornecedor?.Id,
                conta.ClienteId,
                conta.OrigemTipo.ToString(),
                conta.CondicaoPagamento?.Id,
                conta.OrigemId
            ),
            transaction: _session.Transaction);

        await InserirParcelas(idGerado, conta.ContasPagarParcelas);

        var created = new ContasPagar(idGerado, conta.Descricao, conta.ValorOriginal, conta.Fornecedor, conta.DataEmissao, conta.DataVencimento, conta.CondicaoPagamento, conta.OrigemTipo, conta.OrigemId, conta.Observacao, conta.CriadoEm, conta.Status, conta.ClienteId, conta.ClienteNome);
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
                valor_original = @ValorOriginal, valor_saldo = @ValorSaldo, status = @Status::status_titulo_financeiro_enum,
                observacao = @Observacao,
                fornecedor_id = @FornecedorId, cliente_id = @ClienteId, condicao_pagamento_id = @CondicaoPagamentoId
            WHERE id = @Id;";

        await _session.Connection.ExecuteAsync(
            sql,
            new ContaPagarDbRow(
                id,
                conta.Descricao,
                conta.DataEmissao,
                conta.DataVencimento,
                conta.ValorOriginal,
                conta.ValorSaldo,
                conta.Status.ToString(),
                conta.Observacao,
                conta.CriadoEm,
                conta.Fornecedor?.Id,
                conta.ClienteId,
                conta.OrigemTipo.ToString(),
                conta.CondicaoPagamento?.Id,
                conta.OrigemId
            ),
            transaction: _session.Transaction);

        await ReplacerParcelas(id, conta.ContasPagarParcelas);

        var updated = new ContasPagar(id, conta.Descricao, conta.ValorOriginal, conta.Fornecedor, conta.DataEmissao, conta.DataVencimento, conta.CondicaoPagamento, conta.OrigemTipo, conta.OrigemId, conta.Observacao, conta.CriadoEm, conta.Status, conta.ClienteId, conta.ClienteNome);
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

    public async Task<ResultadoPaginado<ContasPagar>> PesquisarContasPagar(string termo, int pagina = 1, int tamanhoDaPagina = 20, FiltroContasPagar? filtro = null)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;
        var f0 = new { Termo = $"%{termo}%", filtro?.FornecedorId, filtro?.ClienteId, OrigemTipo = filtro?.OrigemTipo?.ToString(), filtro?.OrigemId };

        const string countSql = @"
            SELECT COUNT(*)
            FROM contas_pagar cp
            LEFT JOIN fornecedores f ON f.id = cp.fornecedor_id
            LEFT JOIN clientes cli ON cli.id = cp.cliente_id
            WHERE (cp.descricao ILIKE @Termo OR f.nome_razaosocial ILIKE @Termo OR cli.nome_razaosocial ILIKE @Termo)
              AND (@FornecedorId::int IS NULL OR cp.fornecedor_id = @FornecedorId)
              AND (@ClienteId::int IS NULL OR cp.cliente_id = @ClienteId)
              AND (@OrigemTipo::text IS NULL OR cp.origem_tipo::text = @OrigemTipo)
              AND (@OrigemId::int IS NULL OR cp.origem_id = @OrigemId);";

        const string querySql = @"
            SELECT cp.id AS Id, cp.descricao AS Descricao, cp.data_emissao AS DataEmissao, cp.data_vencimento AS DataVencimento, cp.valor_original AS ValorOriginal,
                   cp.valor_saldo AS ValorSaldo, cp.status AS Status, cp.observacao AS Observacao, cp.criado_em AS CriadoEm,
                   cp.origem_tipo::text AS OrigemTipo, cp.origem_id AS OrigemId, cp.cliente_id AS ClienteId, cli.nome_razaosocial AS ClienteNome,
                   f.id AS Id, f.tipo_pessoa AS TipoPessoa, f.nome_razaosocial AS NomeRazaosocial, f.cpf_cnpj AS CpfCnpj,
                   f.rg_ie AS RgIe, f.apelido_nomefantasia AS ApelidoNomefantasia, f.logradouro AS Logradouro, f.numero AS Numero,
                   f.telefone AS Telefone, f.email AS Email, f.ativo AS Ativo, f.criado_em AS CriadoEm, f.observacao AS Observacao,
                   p.id AS Id, p.ddi AS Ddi, p.codigo_iso_pais AS CodigoIsoPais, p.codigo_iso_moeda AS CodigoIsoMoeda, p.simbolo_moeda AS SimboloMoeda, p.pais AS Pais,
                   con.id AS Id, con.descricao AS Descricao, con.entrada_minima_percentual AS EntradaMinimaPercentual,
                   con.desconto_percentual AS DescontoPercentual, con.acrescimo_percentual AS AcrescimoPercentual,
                   con.multa_percentual AS MultaPercentual, con.taxa_juros_percentual AS TaxaJurosPercentual, con.ativo AS Ativo,
                   mp.codigo AS Codigo, mp.descricao AS Descricao, mp.ativo AS Ativo
            FROM contas_pagar cp
            LEFT JOIN fornecedores f ON f.id = cp.fornecedor_id
            LEFT JOIN paises p ON p.id = f.nacionalidade_id
            LEFT JOIN clientes cli ON cli.id = cp.cliente_id
            LEFT JOIN condicoes_pagamentos con ON con.id = cp.condicao_pagamento_id
            LEFT JOIN metodos_pagamento mp ON mp.codigo = con.metodo_pagamento_codigo
            WHERE (cp.descricao ILIKE @Termo OR f.nome_razaosocial ILIKE @Termo OR cli.nome_razaosocial ILIKE @Termo)
              AND (@FornecedorId::int IS NULL OR cp.fornecedor_id = @FornecedorId)
              AND (@ClienteId::int IS NULL OR cp.cliente_id = @ClienteId)
              AND (@OrigemTipo::text IS NULL OR cp.origem_tipo::text = @OrigemTipo)
              AND (@OrigemId::int IS NULL OR cp.origem_id = @OrigemId)
            ORDER BY cp.criado_em DESC, cp.id DESC
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        var total = await _session.Connection.ExecuteScalarAsync<int>(
            countSql,
            f0,
            transaction: _session.Transaction);

        var contas = (await _session.Connection.QueryAsync<ContasPagar, Fornecedores, Paises, CondicoesPagamentos, MetodosPagamentos, ContasPagar>(
            querySql,
            (conta, fornecedor, pais, condicao, metodo) =>
            {
                var f = fornecedor is null ? null : new Fornecedores(
                    fornecedor.Id,
                    fornecedor.TipoPessoa,
                    fornecedor.NomeRazaosocial,
                    fornecedor.CpfCnpj,
                    pais,
                    fornecedor.RgIe,
                    fornecedor.ApelidoNomefantasia,
                    fornecedor.Logradouro,
                    fornecedor.Numero,
                    null,
                    fornecedor.Telefone,
                    fornecedor.Email,
                    fornecedor.Observacao,
                    fornecedor.Ativo,
                    fornecedor.CriadoEm
                );

                if (condicao != null && metodo != null)
                {
                    condicao.AtualizarMetodoPagamento(metodo);
                }

                return new ContasPagar(
                    conta.Id,
                    conta.Descricao,
                    conta.ValorOriginal,
                    f,
                    conta.DataEmissao,
                    conta.DataVencimento,
                    condicao,
                    conta.OrigemTipo,
                    conta.OrigemId,
                    conta.Observacao,
                    conta.CriadoEm,
                    conta.Status,
                    conta.ClienteId,
                    conta.ClienteNome
                );
            },
            new { f0.Termo, f0.FornecedorId, f0.ClienteId, f0.OrigemTipo, f0.OrigemId, TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction,
            splitOn: "Id,Id,Id,Codigo"
        )).ToList();

        if (contas.Count > 0)
        {
            var ids = contas.Select(c => c.Id).ToArray();

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

            foreach (var conta in contas)
            {
                if (parcelasPorConta.TryGetValue(conta.Id, out var sub))
                {
                    foreach (var parcelaDto in sub)
                    {
                        conta.AdicionarParcelaExistente(BuildParcelaPagar(parcelaDto));
                    }
                }
            }
        }

        return new ResultadoPaginado<ContasPagar>(contas, total, pagina, tamanhoDaPagina);
    }

    private async Task InserirParcelas(int contaId, IEnumerable<ContasPagarParcelas> parcelas)
    {
        const string sql = @"
            INSERT INTO contas_pagar_parcelas (numero_parcela, data_vencimento, valor_parcela, valor_pago, status, conta_pagar_id)
            VALUES (@NumeroParcela, @DataVencimento, @ValorParcela, @ValorPago, @Status::status_titulo_financeiro_enum, @ContaId);";

        await _session.Connection.ExecuteAsync(
            sql,
            parcelas.Select(p => new ParcelaPagarDbRow(
                p.NumeroParcela,
                p.DataVencimento,
                p.ValorParcela,
                p.ValorPago,
                p.Status.ToString(),
                contaId
            )).ToList(),
            transaction: _session.Transaction);
    }

    private async Task ReplacerParcelas(int contaId, IEnumerable<ContasPagarParcelas> parcelas)
    {
        await _session.Connection.ExecuteAsync(
            "DELETE FROM contas_pagar_parcelas WHERE conta_pagar_id = @ContaId;",
            new { ContaId = contaId }, transaction: _session.Transaction);

        await InserirParcelas(contaId, parcelas);
    }

    private static ContasPagarParcelas BuildParcelaPagar(ParcelaPagarDto dto)
    {
        return new ContasPagarParcelas(dto.Id, dto.ContaPagarId, dto.NumeroParcela, dto.DataVencimento, dto.ValorParcela, dto.ValorPago, dto.Status);
    }

    private sealed record ParcelaPagarDto(
        int Id,
        int ContaPagarId,
        int NumeroParcela,
        DateTime DataVencimento,
        decimal ValorParcela,
        decimal ValorPago,
        StatusTituloFinanceiro Status);

    private sealed record ContaPagarDbRow(
        int Id,
        string Descricao,
        DateTime? DataEmissao,
        DateTime? DataVencimento,
        decimal ValorOriginal,
        decimal ValorSaldo,
        string Status,
        string? Observacao,
        DateTime CriadoEm,
        int? FornecedorId,
        int? ClienteId,
        string OrigemTipo,
        int? CondicaoPagamentoId,
        int? OrigemId);

    private sealed record ParcelaPagarDbRow(
        int NumeroParcela,
        DateTime DataVencimento,
        decimal ValorParcela,
        decimal ValorPago,
        string Status,
        int ContaId);
}


