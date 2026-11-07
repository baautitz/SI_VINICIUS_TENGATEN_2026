using System.Linq;
using Backend.Core.Common.Results;
using Backend.Core.Features.Localizacao.Entities;
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
    private readonly IOrdenacaoSolicitada? _ordenacao;

    public ContasReceberRepository(DbSession session, IOrdenacaoSolicitada? ordenacao = null)
    {
        _session = session;
        _ordenacao = ordenacao;
    }

    // Lista branca dos campos que a tela pode pedir (nome na API -> expressão SQL).
    private static readonly IReadOnlyDictionary<string, string> CamposOrdenaveis = new Dictionary<string, string>
    {
        ["documento"] = "cr.numero_documento",
        ["parcela"] = "cr.numero_parcela",
        ["descricao"] = "unaccent(lower(cr.descricao))",
        ["cliente"] = "unaccent(lower(COALESCE(c.nome_razaosocial, f.nome_razaosocial)))",
        ["vencimento"] = "cr.data_vencimento",
        ["valor"] = "cr.valor",
        ["valorPago"] = "cr.valor_pago",
        ["valorAbatido"] = "cr.valor_abatido",
        ["valorSaldo"] = "(cr.valor - cr.valor_pago - cr.valor_abatido)",
        ["status"] = "cr.status",
    };

    private const string SelectSql = @"
        SELECT cr.id AS Id, cr.descricao AS Descricao, cr.data_emissao AS DataEmissao, cr.data_vencimento AS DataVencimento,
               cr.valor AS Valor, cr.valor_pago AS ValorPago, cr.valor_abatido AS ValorAbatido, cr.status AS Status, cr.observacao AS Observacao, cr.criado_em AS CriadoEm,
               cr.origem_tipo::text AS OrigemTipo, cr.origem_id AS OrigemId, cr.numero_documento AS NumeroDocumento,
               cr.numero_parcela AS NumeroParcela, cr.total_parcelas AS TotalParcelas,
               cr.fornecedor_id AS FornecedorId, f.nome_razaosocial AS FornecedorNome,
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
        LEFT JOIN clientes c ON c.id = cr.cliente_id
        LEFT JOIN fornecedores f ON f.id = cr.fornecedor_id
        LEFT JOIN paises p ON p.id = c.nacionalidade_id
        LEFT JOIN condicoes_pagamentos con ON con.id = cr.condicao_pagamento_id
        LEFT JOIN metodos_pagamento mp ON mp.codigo = con.metodo_pagamento_codigo";

    private Task<IEnumerable<ContasReceber>> Consultar(string sqlComWhereEOrder, object param)
        => _session.Connection.QueryAsync<ContasReceber, Clientes, Paises, CondicoesPagamentos, MetodosPagamentos, ContasReceber>(
            SelectSql + sqlComWhereEOrder,
            (conta, cliente, pais, condicao, metodo) =>
            {
                var cl = cliente is null ? null : new Clientes(
                    cliente.Id, cliente.TipoPessoa, cliente.NomeRazaoSocial, cliente.CpfCnpj, pais,
                    cliente.RgIe, cliente.ApelidoNomeFantasia, cliente.Logradouro, cliente.Numero, null,
                    cliente.Telefone, cliente.Email, cliente.LimiteCredito, cliente.Observacao, cliente.Ativo,
                    cliente.Sexo, cliente.DataNascimento, cliente.CriadoEm);

                if (condicao != null && metodo != null)
                    condicao.AtualizarMetodoPagamento(metodo);

                return new ContasReceber(
                    conta.Id, conta.Descricao, conta.Valor, cl, conta.DataVencimento, conta.NumeroDocumento,
                    conta.NumeroParcela, conta.TotalParcelas, conta.DataEmissao, condicao, conta.OrigemTipo, conta.OrigemId,
                    conta.Observacao, conta.CriadoEm, conta.ValorPago, conta.Status, conta.ValorAbatido, conta.FornecedorId, conta.FornecedorNome);
            },
            param,
            transaction: _session.Transaction,
            splitOn: "Id,Id,Id,Codigo");

    public async Task<ContasReceber?> ObterContaReceberPorId(int id)
        => (await Consultar(" WHERE cr.id = @Id;", new { Id = id })).SingleOrDefault();

    public async Task<ContasReceber?> ObterContaReceberPorIdParaAtualizar(int id)
        => (await Consultar(" WHERE cr.id = @Id FOR UPDATE OF cr;", new { Id = id })).SingleOrDefault();

    public async Task<IReadOnlyList<ContasReceber>> ObterParcelasManuaisDoDocumentoParaAtualizar(int id)
        => (await Consultar(@"
            WHERE cr.origem_tipo::text = 'MANUAL'
              AND cr.numero_documento = (SELECT numero_documento FROM contas_receber WHERE id = @Id)
              AND cr.cliente_id = (SELECT cliente_id FROM contas_receber WHERE id = @Id)
            ORDER BY cr.numero_parcela FOR UPDATE OF cr;", new { Id = id })).ToList();

    public async Task<ContasReceber> CriarContaReceber(ContasReceber conta)
    {
        const string sql = @"
            INSERT INTO contas_receber (origem_tipo, origem_id, numero_documento, numero_parcela, total_parcelas,
                                        cliente_id, fornecedor_id, descricao, data_emissao, data_vencimento,
                                        valor, valor_pago, valor_abatido, status, condicao_pagamento_id, observacao, criado_em, atualizado_em)
            VALUES (@OrigemTipo::origem_titulo_financeiro_enum, @OrigemId, @NumeroDocumento, @NumeroParcela, @TotalParcelas,
                    @ClienteId, @FornecedorId, @Descricao, @DataEmissao, @DataVencimento,
                    @Valor, @ValorPago, @ValorAbatido, @Status::status_titulo_financeiro_enum, @CondicaoPagamentoId, @Observacao, @CriadoEm, @CriadoEm)
            RETURNING id;";

        var id = await _session.Connection.ExecuteScalarAsync<int>(sql, Parametros(0, conta), transaction: _session.Transaction);

        return new ContasReceber(id, conta.Descricao, conta.Valor, conta.Cliente, conta.DataVencimento, conta.NumeroDocumento,
            conta.NumeroParcela, conta.TotalParcelas, conta.DataEmissao, conta.CondicaoPagamento, conta.OrigemTipo, conta.OrigemId,
            conta.Observacao, conta.CriadoEm, conta.ValorPago, conta.Status, conta.ValorAbatido, conta.FornecedorId, conta.FornecedorNome);
    }

    public async Task<ContasReceber> AtualizarContaReceber(int id, ContasReceber conta)
    {
        const string sql = @"
            UPDATE contas_receber
            SET descricao = @Descricao, data_vencimento = @DataVencimento, valor = @Valor, valor_pago = @ValorPago, valor_abatido = @ValorAbatido,
                status = @Status::status_titulo_financeiro_enum, observacao = @Observacao, atualizado_em = NOW()
            WHERE id = @Id;";

        await _session.Connection.ExecuteAsync(sql, Parametros(id, conta), transaction: _session.Transaction);
        return conta;
    }

    public async Task<bool> DeletarContaReceber(int id)
    {
        var linhasAfetadas = await _session.Connection.ExecuteAsync(
            "DELETE FROM contas_receber WHERE id = @Id;",
            new { Id = id }, transaction: _session.Transaction);

        return linhasAfetadas > 0;
    }

    public async Task<ResultadoPaginado<ContasReceber>> PesquisarContasReceber(string termo, int pagina = 1, int tamanhoDaPagina = 20, FiltroContasReceber? filtro = null)
    {
        var param = PesquisaTitulos.Parametros(termo, filtro, pagina, tamanhoDaPagina);

        var where = PesquisaTitulos.Where("cr.texto_pesquisa", PesquisaTitulos.Analisar(termo)) + @"
              " + PesquisaTitulos.FiltroComum("cr") + @"
              AND (@CpfCnpj::text IS NULL OR @CpfCnpj IN (regexp_replace(COALESCE(c.cpf_cnpj, ''), '\D', '', 'g'), regexp_replace(COALESCE(f.cpf_cnpj, ''), '\D', '', 'g')))";

        await PesquisaTitulos.AjustarLimiar(_session.Connection, _session.Transaction);
        var total = await _session.Connection.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*)
            FROM contas_receber cr
            LEFT JOIN clientes c ON c.id = cr.cliente_id
            LEFT JOIN fornecedores f ON f.id = cr.fornecedor_id" + where + ";",
            param, transaction: _session.Transaction);

        // Escolha do usuário no cabeçalho; sem ela vale a relevância da pesquisa e depois o que exige ação.
        var escolhida = OrdenacaoListagem.MontarOrderBy(_ordenacao?.OrdenarPor, _ordenacao?.Direcao, CamposOrdenaveis, "cr.id");
        var contas = (await Consultar(where + @"
            ORDER BY " + (escolhida ?? PesquisaTitulos.Relevancia("cr.texto_pesquisa", "cr.numero_documento", "cr.descricao") + @",
                     CASE WHEN cr.status::text IN ('PAGO', 'CANCELADO') THEN 1 ELSE 0 END, cr.data_vencimento, cr.numero_documento, cr.numero_parcela, cr.id")
            + @"
            LIMIT @TamanhoDaPagina OFFSET @Offset;", param)).ToList();

        return new ResultadoPaginado<ContasReceber>(contas, total, pagina, tamanhoDaPagina);
    }

    private static object Parametros(int id, ContasReceber c) => new
    {
        Id = id,
        OrigemTipo = c.OrigemTipo.ToString(),
        c.OrigemId,
        c.NumeroDocumento,
        c.NumeroParcela,
        c.TotalParcelas,
        ClienteId = c.Cliente?.Id,
        c.FornecedorId,
        c.Descricao,
        c.DataEmissao,
        c.DataVencimento,
        c.Valor,
        c.ValorPago,
        c.ValorAbatido,
        Status = c.Status.ToString(),
        CondicaoPagamentoId = c.CondicaoPagamento?.Id,
        c.Observacao,
        c.CriadoEm
    };
}
