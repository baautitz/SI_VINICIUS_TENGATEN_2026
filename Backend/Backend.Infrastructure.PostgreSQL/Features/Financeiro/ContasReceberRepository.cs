using System.Linq;
using Backend.Core.Common.Results;
using Backend.Core.Features.Parceiros.Enums;
using Backend.Core.Common.ValueObjects;
using Backend.Core.Features.Localizacao.Entities;
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
            SELECT cr.id AS Id, cr.descricao AS Descricao, cr.data_emissao AS DataEmissao, cr.data_vencimento AS DataVencimento, cr.valor_original AS ValorOriginal,
                   cr.valor_saldo AS ValorSaldo, cr.status AS Status, cr.observacao AS Observacao, cr.criado_em AS CriadoEm,
                   cr.nfe_id AS NfeId, cr.venda_id AS VendaId,
                   c.id AS Id, c.tipo_pessoa AS TipoPessoa, c.nome_razaosocial AS NomeRazaoSocial, c.cpf_cnpj AS CpfCnpj,
                   c.rg_ie AS RgIe, c.apelido_nomefantasia AS ApelidoNomeFantasia, c.logradouro AS Logradouro, c.numero AS Numero,
                   c.telefone AS Telefone, c.email AS Email, c.limite_credito AS LimiteCredito,
                   c.ativo AS Ativo, c.criado_em AS CriadoEm, c.observacao AS Observacao,
                   p.id AS Id, p.ddi AS Ddi, p.codigo_iso_pais AS CodigoIsoPais, p.codigo_iso_moeda AS CodigoIsoMoeda, p.simbolo_moeda AS SimboloMoeda, p.pais AS Pais,
                   con.id AS Id, con.descricao AS Descricao, con.entrada_minima_percentual AS EntradaMinimaPercentual,
                   con.desconto_percentual AS DescontoPercentual, con.acrescimo_percentual AS AcrescimoPercentual,
                   con.multa_percentual AS MultaPercentual, con.taxa_juros_percentual AS TaxaJurosPercentual, con.ativo AS Ativo,
                   mp.codigo AS Codigo, mp.descricao AS Descricao, mp.ativo AS Ativo
            FROM contas_receber cr
            JOIN clientes c ON c.id = cr.cliente_id
            JOIN paises p ON p.id = c.nacionalidade_id
            LEFT JOIN condicoes_pagamentos con ON con.id = cr.condicao_pagamento_id
            LEFT JOIN metodos_pagamento mp ON mp.codigo = con.metodo_pagamento_codigo
            ORDER BY cr.criado_em DESC, cr.id DESC
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        var total = await _session.Connection.ExecuteScalarAsync<int>(
            countSql, transaction: _session.Transaction);

        var types = new[] { typeof(ContasReceber), typeof(Clientes), typeof(Paises), typeof(CondicoesPagamentos), typeof(MetodosPagamentos) };

        var rawContas = await _session.Connection.QueryAsync(
            querySql,
            types,
            obj =>
            {
                var conta = (ContasReceber)obj[0];
                var cliente = (Clientes)obj[1];
                var pais = (Paises)obj[2];
                var condicao = (CondicoesPagamentos)obj[3];
                var metodo = (MetodosPagamentos)obj[4];

                var cl = new Clientes(
                    cliente.Id,
                    cliente.TipoPessoa,
                    cliente.NomeRazaoSocial,
                    cliente.CpfCnpj,
                    pais,
                    cliente.RgIe,
                    cliente.ApelidoNomeFantasia,
                    cliente.Logradouro,
                    cliente.Numero,
                    null,
                    cliente.Telefone,
                    cliente.Email,
                    cliente.LimiteCredito,
                    cliente.Observacao,
                    cliente.Ativo,
                    cliente.Sexo,
                    cliente.DataNascimento,
                    cliente.CriadoEm
                );

                if (condicao != null && metodo != null)
                {
                    condicao.AtualizarMetodoPagamento(metodo);
                }

                return new ContasReceber(
                    conta.Id,
                    conta.Descricao,
                    conta.ValorOriginal,
                    cl,
                    conta.DataEmissao,
                    conta.DataVencimento,
                    condicao,
                    conta.NfeId,
                    conta.VendaId,
                    conta.Observacao,
                    conta.CriadoEm,
                    conta.Status
                );
            },
            new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction,
            splitOn: "Id,Id,Id,Codigo"
        );

        var contas = rawContas.ToList();

        if (contas.Count > 0)
        {
            var ids = contas.Select(c => c.Id).ToArray();

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

            foreach (var conta in contas)
            {
                if (parcelasPorConta.TryGetValue(conta.Id, out var sub))
                {
                    foreach (var parcelaDto in sub)
                    {
                        conta.AdicionarParcelaExistente(BuildParcelaReceber(parcelaDto));
                    }
                }
            }
        }

        return new ResultadoPaginado<ContasReceber>(contas, total, pagina, tamanhoDaPagina);
    }

    public async Task<ContasReceber?> ObterContaReceberPorId(int id)
    {
        const string contaSql = @"
            SELECT cr.id AS Id, cr.descricao AS Descricao, cr.data_emissao AS DataEmissao, cr.data_vencimento AS DataVencimento, cr.valor_original AS ValorOriginal,
                   cr.valor_saldo AS ValorSaldo, cr.status AS Status, cr.observacao AS Observacao, cr.criado_em AS CriadoEm,
                   cr.nfe_id AS NfeId, cr.venda_id AS VendaId,
                   c.id AS Id, c.tipo_pessoa AS TipoPessoa, c.nome_razaosocial AS NomeRazaoSocial, c.cpf_cnpj AS CpfCnpj,
                   c.rg_ie AS RgIe, c.apelido_nomefantasia AS ApelidoNomeFantasia, c.logradouro AS Logradouro, c.numero AS Numero,
                   c.telefone AS Telefone, c.email AS Email, c.limite_credito AS LimiteCredito,
                   c.ativo AS Ativo, c.criado_em AS CriadoEm, c.observacao AS Observacao,
                   p.id AS Id, p.ddi AS Ddi, p.codigo_iso_pais AS CodigoIsoPais, p.codigo_iso_moeda AS CodigoIsoMoeda, p.simbolo_moeda AS SimboloMoeda, p.pais AS Pais,
                   con.id AS Id, con.descricao AS Descricao, con.entrada_minima_percentual AS EntradaMinimaPercentual,
                   con.desconto_percentual AS DescontoPercentual, con.acrescimo_percentual AS AcrescimoPercentual,
                   con.multa_percentual AS MultaPercentual, con.taxa_juros_percentual AS TaxaJurosPercentual, con.ativo AS Ativo,
                   mp.codigo AS Codigo, mp.descricao AS Descricao, mp.ativo AS Ativo
            FROM contas_receber cr
            JOIN clientes c ON c.id = cr.cliente_id
            JOIN paises p ON p.id = c.nacionalidade_id
            LEFT JOIN condicoes_pagamentos con ON con.id = cr.condicao_pagamento_id
            LEFT JOIN metodos_pagamento mp ON mp.codigo = con.metodo_pagamento_codigo
            WHERE cr.id = @Id;";

        const string parcelasSql = @"
            SELECT id, conta_receber_id AS ContaReceberId, numero_parcela AS NumeroParcela, data_vencimento AS DataVencimento,
                   valor_parcela AS ValorParcela, valor_recebido AS ValorRecebido, status AS Status
            FROM contas_receber_parcelas
            WHERE conta_receber_id = @Id
            ORDER BY numero_parcela;";

        var types = new[] { typeof(ContasReceber), typeof(Clientes), typeof(Paises), typeof(CondicoesPagamentos), typeof(MetodosPagamentos) };

        var rawConta = await _session.Connection.QueryAsync(
            contaSql,
            types,
            obj =>
            {
                var conta = (ContasReceber)obj[0];
                var cliente = (Clientes)obj[1];
                var pais = (Paises)obj[2];
                var condicao = (CondicoesPagamentos)obj[3];
                var metodo = (MetodosPagamentos)obj[4];

                var cl = new Clientes(
                    cliente.Id,
                    cliente.TipoPessoa,
                    cliente.NomeRazaoSocial,
                    cliente.CpfCnpj,
                    pais,
                    cliente.RgIe,
                    cliente.ApelidoNomeFantasia,
                    cliente.Logradouro,
                    cliente.Numero,
                    null,
                    cliente.Telefone,
                    cliente.Email,
                    cliente.LimiteCredito,
                    cliente.Observacao,
                    cliente.Ativo,
                    cliente.Sexo,
                    cliente.DataNascimento,
                    cliente.CriadoEm
                );

                if (condicao != null && metodo != null)
                {
                    condicao.AtualizarMetodoPagamento(metodo);
                }

                return new ContasReceber(
                    conta.Id,
                    conta.Descricao,
                    conta.ValorOriginal,
                    cl,
                    conta.DataEmissao,
                    conta.DataVencimento,
                    condicao,
                    conta.NfeId,
                    conta.VendaId,
                    conta.Observacao,
                    conta.CriadoEm,
                    conta.Status
                );
            },
            new { Id = id },
            transaction: _session.Transaction,
            splitOn: "Id,Id,Id,Codigo"
        );

        var conta = rawConta.SingleOrDefault();

        if (conta is null) return null;

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
                                       status, observacao, criado_em, cliente_id, nfe_id, condicao_pagamento_id, venda_id)
            VALUES (@Descricao, @DataEmissao, @DataVencimento, @ValorOriginal, @ValorSaldo,
                    @Status::status_titulo_financeiro_enum, @Observacao, @CriadoEm, @ClienteId, @NfeId, @CondicaoPagamentoId, @VendaId)
            RETURNING id;";

        var idGerado = await _session.Connection.ExecuteScalarAsync<int>(
            sql,
            new ContaReceberDbRow(
                0,
                conta.Descricao,
                conta.DataEmissao,
                conta.DataVencimento,
                conta.ValorOriginal,
                conta.ValorSaldo,
                conta.Status.ToString(),
                conta.Observacao,
                DateTime.UtcNow,
                conta.Cliente.Id,
                conta.NfeId,
                conta.CondicaoPagamento?.Id,
                conta.VendaId
            ),
            transaction: _session.Transaction);

        await InserirParcelas(idGerado, conta.ContasReceberParcelas);

        var created = new ContasReceber(idGerado, conta.Descricao, conta.ValorOriginal, conta.Cliente, conta.DataEmissao, conta.DataVencimento, conta.CondicaoPagamento, conta.NfeId, conta.VendaId, conta.Observacao, conta.CriadoEm, conta.Status);
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
                valor_original = @ValorOriginal, valor_saldo = @ValorSaldo, status = @Status::status_titulo_financeiro_enum,
                observacao = @Observacao,
                cliente_id = @ClienteId, nfe_id = @NfeId, condicao_pagamento_id = @CondicaoPagamentoId, venda_id = @VendaId
            WHERE id = @Id;";

        await _session.Connection.ExecuteAsync(
            sql,
            new ContaReceberDbRow(
                id,
                conta.Descricao,
                conta.DataEmissao,
                conta.DataVencimento,
                conta.ValorOriginal,
                conta.ValorSaldo,
                conta.Status.ToString(),
                conta.Observacao,
                conta.CriadoEm,
                conta.Cliente.Id,
                conta.NfeId,
                conta.CondicaoPagamento?.Id,
                conta.VendaId
            ),
            transaction: _session.Transaction);

        await ReplacerParcelas(id, conta.ContasReceberParcelas);

        var updated = new ContasReceber(id, conta.Descricao, conta.ValorOriginal, conta.Cliente, conta.DataEmissao, conta.DataVencimento, conta.CondicaoPagamento, conta.NfeId, conta.VendaId, conta.Observacao, conta.CriadoEm, conta.Status);
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

    public async Task<ResultadoPaginado<ContasReceber>> PesquisarContasReceber(string termo, int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string countSql = @"
            SELECT COUNT(*)
            FROM contas_receber cr
            JOIN clientes c ON c.id = cr.cliente_id
            WHERE cr.descricao ILIKE @Termo OR c.nome_razaosocial ILIKE @Termo;";

        const string querySql = @"
            SELECT cr.id AS Id, cr.descricao AS Descricao, cr.data_emissao AS DataEmissao, cr.data_vencimento AS DataVencimento, cr.valor_original AS ValorOriginal,
                   cr.valor_saldo AS ValorSaldo, cr.status AS Status, cr.observacao AS Observacao, cr.criado_em AS CriadoEm,
                   cr.nfe_id AS NfeId, cr.venda_id AS VendaId,
                   c.id AS Id, c.tipo_pessoa AS TipoPessoa, c.nome_razaosocial AS NomeRazaoSocial, c.cpf_cnpj AS CpfCnpj,
                   c.rg_ie AS RgIe, c.apelido_nomefantasia AS ApelidoNomeFantasia, c.logradouro AS Logradouro, c.numero AS Numero,
                   c.telefone AS Telefone, c.email AS Email, c.limite_credito AS LimiteCredito,
                   c.ativo AS Ativo, c.criado_em AS CriadoEm, c.observacao AS Observacao,
                   p.id AS Id, p.ddi AS Ddi, p.codigo_iso_pais AS CodigoIsoPais, p.codigo_iso_moeda AS CodigoIsoMoeda, p.simbolo_moeda AS SimboloMoeda, p.pais AS Pais,
                   con.id AS Id, con.descricao AS Descricao, con.entrada_minima_percentual AS EntradaMinimaPercentual,
                   con.desconto_percentual AS DescontoPercentual, con.acrescimo_percentual AS AcrescimoPercentual,
                   con.multa_percentual AS MultaPercentual, con.taxa_juros_percentual AS TaxaJurosPercentual, con.ativo AS Ativo,
                   mp.codigo AS Codigo, mp.descricao AS Descricao, mp.ativo AS Ativo
            FROM contas_receber cr
            JOIN clientes c ON c.id = cr.cliente_id
            JOIN paises p ON p.id = c.nacionalidade_id
            LEFT JOIN condicoes_pagamentos con ON con.id = cr.condicao_pagamento_id
            LEFT JOIN metodos_pagamento mp ON mp.codigo = con.metodo_pagamento_codigo
            WHERE cr.descricao ILIKE @Termo OR c.nome_razaosocial ILIKE @Termo
            ORDER BY cr.criado_em DESC, cr.id DESC
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        var total = await _session.Connection.ExecuteScalarAsync<int>(
            countSql,
            new { Termo = $"%{termo}%" },
            transaction: _session.Transaction);

        var types = new[] { typeof(ContasReceber), typeof(Clientes), typeof(Paises), typeof(CondicoesPagamentos), typeof(MetodosPagamentos) };

        var rawContas = await _session.Connection.QueryAsync(
            querySql,
            types,
            obj =>
            {
                var conta = (ContasReceber)obj[0];
                var cliente = (Clientes)obj[1];
                var pais = (Paises)obj[2];
                var condicao = (CondicoesPagamentos)obj[3];
                var metodo = (MetodosPagamentos)obj[4];

                var cl = new Clientes(
                    cliente.Id,
                    cliente.TipoPessoa,
                    cliente.NomeRazaoSocial,
                    cliente.CpfCnpj,
                    pais,
                    cliente.RgIe,
                    cliente.ApelidoNomeFantasia,
                    cliente.Logradouro,
                    cliente.Numero,
                    null,
                    cliente.Telefone,
                    cliente.Email,
                    cliente.LimiteCredito,
                    cliente.Observacao,
                    cliente.Ativo,
                    cliente.Sexo,
                    cliente.DataNascimento,
                    cliente.CriadoEm
                );

                if (condicao != null && metodo != null)
                {
                    condicao.AtualizarMetodoPagamento(metodo);
                }

                return new ContasReceber(
                    conta.Id,
                    conta.Descricao,
                    conta.ValorOriginal,
                    cl,
                    conta.DataEmissao,
                    conta.DataVencimento,
                    condicao,
                    conta.NfeId,
                    conta.VendaId,
                    conta.Observacao,
                    conta.CriadoEm,
                    conta.Status
                );
            },
            new { Termo = $"%{termo}%", TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction,
            splitOn: "Id,Id,Id,Codigo"
        );

        var contas = rawContas.ToList();

        if (contas.Count > 0)
        {
            var ids = contas.Select(c => c.Id).ToArray();

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

            foreach (var conta in contas)
            {
                if (parcelasPorConta.TryGetValue(conta.Id, out var sub))
                {
                    foreach (var parcelaDto in sub)
                    {
                        conta.AdicionarParcelaExistente(BuildParcelaReceber(parcelaDto));
                    }
                }
            }
        }

        return new ResultadoPaginado<ContasReceber>(contas, total, pagina, tamanhoDaPagina);
    }

    private async Task InserirParcelas(int contaId, IEnumerable<ContasReceberParcelas> parcelas)
    {
        const string sql = @"
            INSERT INTO contas_receber_parcelas (numero_parcela, data_vencimento, valor_parcela, valor_recebido, status, conta_receber_id)
            VALUES (@NumeroParcela, @DataVencimento, @ValorParcela, @ValorRecebido, @Status::status_titulo_financeiro_enum, @ContaId);";

        await _session.Connection.ExecuteAsync(
            sql,
            parcelas.Select(p => new ParcelaReceberDbRow(
                p.NumeroParcela,
                p.DataVencimento,
                p.ValorParcela,
                p.ValorRecebido,
                p.Status.ToString(),
                contaId
            )).ToList(),
            transaction: _session.Transaction);
    }

    private async Task ReplacerParcelas(int contaId, IEnumerable<ContasReceberParcelas> parcelas)
    {
        await _session.Connection.ExecuteAsync(
            "DELETE FROM contas_receber_parcelas WHERE conta_receber_id = @ContaId;",
            new { ContaId = contaId }, transaction: _session.Transaction);

        await InserirParcelas(contaId, parcelas);
    }

    private static ContasReceberParcelas BuildParcelaReceber(ParcelaReceberDto dto)
    {
        return new ContasReceberParcelas(dto.Id, dto.ContaReceberId, dto.NumeroParcela, dto.DataVencimento, dto.ValorParcela, dto.ValorRecebido, dto.Status);
    }

    private sealed record ParcelaReceberDto(
        int Id,
        int ContaReceberId,
        int NumeroParcela,
        DateTime DataVencimento,
        decimal ValorParcela,
        decimal ValorRecebido,
        StatusTituloFinanceiro Status);

    private sealed record ContaReceberDbRow(
        int Id,
        string Descricao,
        DateTime? DataEmissao,
        DateTime? DataVencimento,
        decimal ValorOriginal,
        decimal ValorSaldo,
        string Status,
        string? Observacao,
        DateTime CriadoEm,
        int ClienteId,
        int? NfeId,
        int? CondicaoPagamentoId,
        int? VendaId);

    private sealed record ParcelaReceberDbRow(
        int NumeroParcela,
        DateTime DataVencimento,
        decimal ValorParcela,
        decimal ValorRecebido,
        string Status,
        int ContaId);
}


