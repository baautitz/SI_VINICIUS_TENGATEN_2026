using System.Linq;
using Backend.Core.Common.Results;
using Backend.Core.Common.ValueObjects;
using Backend.Core.Features.Localizacao.Entities;
using Backend.Core.Features.Parceiros.Enums;
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

    private const string SelectSql = @"
        SELECT cp.id AS Id, cp.descricao AS Descricao, cp.data_emissao AS DataEmissao, cp.data_vencimento AS DataVencimento,
               cp.valor AS Valor, cp.valor_pago AS ValorPago, cp.status AS Status, cp.observacao AS Observacao, cp.criado_em AS CriadoEm,
               cp.origem_tipo::text AS OrigemTipo, cp.origem_id AS OrigemId, cp.numero_documento AS NumeroDocumento,
               cp.numero_parcela AS NumeroParcela, cp.total_parcelas AS TotalParcelas,
               cp.cliente_id AS ClienteId, cp.transportadora_id AS TransportadoraId, cli.nome_razaosocial AS ClienteNome, tr.nome_razaosocial AS TransportadoraNome,
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
        LEFT JOIN transportadoras tr ON tr.id = cp.transportadora_id
        LEFT JOIN condicoes_pagamentos con ON con.id = cp.condicao_pagamento_id
        LEFT JOIN metodos_pagamento mp ON mp.codigo = con.metodo_pagamento_codigo";

    private Task<IEnumerable<ContasPagar>> Consultar(string sqlComWhereEOrder, object param)
        => _session.Connection.QueryAsync<ContasPagar, Fornecedores, Paises, CondicoesPagamentos, MetodosPagamentos, ContasPagar>(
            SelectSql + sqlComWhereEOrder,
            (conta, fornecedor, pais, condicao, metodo) =>
            {
                var f = fornecedor is null ? null : new Fornecedores(
                    fornecedor.Id, fornecedor.TipoPessoa, fornecedor.NomeRazaosocial, fornecedor.CpfCnpj, pais,
                    fornecedor.RgIe, fornecedor.ApelidoNomefantasia, fornecedor.Logradouro, fornecedor.Numero, null,
                    fornecedor.Telefone, fornecedor.Email, fornecedor.Observacao, fornecedor.Ativo, fornecedor.CriadoEm);

                if (condicao != null && metodo != null)
                    condicao.AtualizarMetodoPagamento(metodo);

                return new ContasPagar(
                    conta.Id, conta.Descricao, conta.Valor, f, conta.DataVencimento, conta.NumeroDocumento,
                    conta.NumeroParcela, conta.TotalParcelas, conta.DataEmissao, condicao, conta.OrigemTipo, conta.OrigemId,
                    conta.Observacao, conta.CriadoEm, conta.ValorPago, conta.Status, conta.ClienteId, conta.ClienteNome, conta.TransportadoraId, conta.TransportadoraNome);
            },
            param,
            transaction: _session.Transaction,
            splitOn: "Id,Id,Id,Codigo");

    public async Task<ContasPagar?> ObterContaPagarPorId(int id)
        => (await Consultar(" WHERE cp.id = @Id;", new { Id = id })).SingleOrDefault();

    public async Task<ContasPagar?> ObterContaPagarPorIdParaAtualizar(int id)
        => (await Consultar(" WHERE cp.id = @Id FOR UPDATE OF cp;", new { Id = id })).SingleOrDefault();

    public async Task<ContasPagar> CriarContaPagar(ContasPagar conta)
    {
        const string sql = @"
            INSERT INTO contas_pagar (origem_tipo, origem_id, numero_documento, numero_parcela, total_parcelas,
                                      fornecedor_id, cliente_id, transportadora_id, descricao, data_emissao, data_vencimento,
                                      valor, valor_pago, status, condicao_pagamento_id, observacao, criado_em, atualizado_em)
            VALUES (@OrigemTipo::origem_titulo_financeiro_enum, @OrigemId, @NumeroDocumento, @NumeroParcela, @TotalParcelas,
                    @FornecedorId, @ClienteId, @TransportadoraId, @Descricao, @DataEmissao, @DataVencimento,
                    @Valor, @ValorPago, @Status::status_titulo_financeiro_enum, @CondicaoPagamentoId, @Observacao, @CriadoEm, @CriadoEm)
            RETURNING id;";

        var id = await _session.Connection.ExecuteScalarAsync<int>(sql, Parametros(0, conta), transaction: _session.Transaction);

        return new ContasPagar(id, conta.Descricao, conta.Valor, conta.Fornecedor, conta.DataVencimento, conta.NumeroDocumento,
            conta.NumeroParcela, conta.TotalParcelas, conta.DataEmissao, conta.CondicaoPagamento, conta.OrigemTipo, conta.OrigemId,
            conta.Observacao, conta.CriadoEm, conta.ValorPago, conta.Status, conta.ClienteId, conta.ClienteNome, conta.TransportadoraId, conta.TransportadoraNome);
    }

    public async Task<ContasPagar> AtualizarContaPagar(int id, ContasPagar conta)
    {
        const string sql = @"
            UPDATE contas_pagar
            SET descricao = @Descricao, data_vencimento = @DataVencimento, valor = @Valor, valor_pago = @ValorPago,
                status = @Status::status_titulo_financeiro_enum, observacao = @Observacao, atualizado_em = NOW()
            WHERE id = @Id;";

        await _session.Connection.ExecuteAsync(sql, Parametros(id, conta), transaction: _session.Transaction);
        return conta;
    }

    public async Task<bool> DeletarContaPagar(int id)
    {
        var linhasAfetadas = await _session.Connection.ExecuteAsync(
            "DELETE FROM contas_pagar WHERE id = @Id;",
            new { Id = id }, transaction: _session.Transaction);

        return linhasAfetadas > 0;
    }

    public async Task<ResultadoPaginado<ContasPagar>> PesquisarContasPagar(string termo, int pagina = 1, int tamanhoDaPagina = 20, FiltroContasPagar? filtro = null)
    {
        var param = PesquisaTitulos.Parametros(termo, filtro, pagina, tamanhoDaPagina);
        param.Add("FornecedorId", filtro?.FornecedorId);
        param.Add("CompraId", filtro?.CompraId);
        param.Add("TransportadoraId", filtro?.TransportadoraId);

        var where = PesquisaTitulos.Where("cp.texto_pesquisa", PesquisaTitulos.Analisar(termo)) + @"
              AND (@FornecedorId::int IS NULL OR cp.fornecedor_id = @FornecedorId)
              AND (@TransportadoraId::int IS NULL OR cp.transportadora_id = @TransportadoraId)
              AND (@CompraId::int IS NULL OR (cp.origem_id = @CompraId AND cp.origem_tipo::text IN ('COMPRA', 'FRETE_COMPRA')))" + PesquisaTitulos.FiltroComum("cp") + @"
              AND (@CpfCnpj::text IS NULL OR @CpfCnpj IN (regexp_replace(COALESCE(f.cpf_cnpj, ''), '\D', '', 'g'), regexp_replace(COALESCE(cli.cpf_cnpj, ''), '\D', '', 'g')))";

        await PesquisaTitulos.AjustarLimiar(_session.Connection, _session.Transaction);
        var total = await _session.Connection.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*)
            FROM contas_pagar cp
            LEFT JOIN fornecedores f ON f.id = cp.fornecedor_id
            LEFT JOIN clientes cli ON cli.id = cp.cliente_id
            LEFT JOIN transportadoras tr ON tr.id = cp.transportadora_id" + where + ";",
            param, transaction: _session.Transaction);

        var contas = (await Consultar(where + @"
            ORDER BY " + PesquisaTitulos.Relevancia("cp.texto_pesquisa", "cp.numero_documento", "cp.descricao") + @",
                     CASE WHEN cp.status::text IN ('PAGO', 'CANCELADO') THEN 1 ELSE 0 END, cp.data_vencimento, cp.numero_documento, cp.numero_parcela, cp.id
            LIMIT @TamanhoDaPagina OFFSET @Offset;", param)).ToList();

        return new ResultadoPaginado<ContasPagar>(contas, total, pagina, tamanhoDaPagina);
    }

    private static object Parametros(int id, ContasPagar c) => new
    {
        Id = id,
        OrigemTipo = c.OrigemTipo.ToString(),
        c.OrigemId,
        c.NumeroDocumento,
        c.NumeroParcela,
        c.TotalParcelas,
        FornecedorId = c.Fornecedor?.Id,
        c.ClienteId,
        c.TransportadoraId,
        c.Descricao,
        c.DataEmissao,
        c.DataVencimento,
        c.Valor,
        c.ValorPago,
        Status = c.Status.ToString(),
        CondicaoPagamentoId = c.CondicaoPagamento?.Id,
        c.Observacao,
        c.CriadoEm
    };
}
