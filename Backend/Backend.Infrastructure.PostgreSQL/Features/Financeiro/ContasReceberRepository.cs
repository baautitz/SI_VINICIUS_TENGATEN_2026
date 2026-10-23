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

    public ContasReceberRepository(DbSession session)
    {
        _session = session;
    }

    private const string SelectSql = @"
        SELECT cr.id AS Id, cr.descricao AS Descricao, cr.data_emissao AS DataEmissao, cr.data_vencimento AS DataVencimento,
               cr.valor AS Valor, cr.valor_pago AS ValorPago, cr.status AS Status, cr.observacao AS Observacao, cr.criado_em AS CriadoEm,
               cr.origem_tipo::text AS OrigemTipo, cr.origem_id AS OrigemId, cr.numero_documento AS NumeroDocumento,
               cr.numero_parcela AS NumeroParcela, cr.total_parcelas AS TotalParcelas,
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
        LEFT JOIN metodos_pagamento mp ON mp.codigo = con.metodo_pagamento_codigo";

    private Task<IEnumerable<ContasReceber>> Consultar(string sqlComWhereEOrder, object param)
        => _session.Connection.QueryAsync<ContasReceber, Clientes, Paises, CondicoesPagamentos, MetodosPagamentos, ContasReceber>(
            SelectSql + sqlComWhereEOrder,
            (conta, cliente, pais, condicao, metodo) =>
            {
                var cl = new Clientes(
                    cliente.Id, cliente.TipoPessoa, cliente.NomeRazaoSocial, cliente.CpfCnpj, pais,
                    cliente.RgIe, cliente.ApelidoNomeFantasia, cliente.Logradouro, cliente.Numero, null,
                    cliente.Telefone, cliente.Email, cliente.LimiteCredito, cliente.Observacao, cliente.Ativo,
                    cliente.Sexo, cliente.DataNascimento, cliente.CriadoEm);

                if (condicao != null && metodo != null)
                    condicao.AtualizarMetodoPagamento(metodo);

                return new ContasReceber(
                    conta.Id, conta.Descricao, conta.Valor, cl, conta.DataVencimento, conta.NumeroDocumento,
                    conta.NumeroParcela, conta.TotalParcelas, conta.DataEmissao, condicao, conta.OrigemTipo, conta.OrigemId,
                    conta.Observacao, conta.CriadoEm, conta.ValorPago, conta.Status);
            },
            param,
            transaction: _session.Transaction,
            splitOn: "Id,Id,Id,Codigo");

    public async Task<ContasReceber?> ObterContaReceberPorId(int id)
        => (await Consultar(" WHERE cr.id = @Id;", new { Id = id })).SingleOrDefault();

    public async Task<ContasReceber?> ObterContaReceberPorIdParaAtualizar(int id)
        => (await Consultar(" WHERE cr.id = @Id FOR UPDATE OF cr;", new { Id = id })).SingleOrDefault();

    public async Task<ContasReceber> CriarContaReceber(ContasReceber conta)
    {
        const string sql = @"
            INSERT INTO contas_receber (origem_tipo, origem_id, numero_documento, numero_parcela, total_parcelas,
                                        cliente_id, descricao, data_emissao, data_vencimento,
                                        valor, valor_pago, status, condicao_pagamento_id, observacao, criado_em, atualizado_em)
            VALUES (@OrigemTipo::origem_titulo_financeiro_enum, @OrigemId, @NumeroDocumento, @NumeroParcela, @TotalParcelas,
                    @ClienteId, @Descricao, @DataEmissao, @DataVencimento,
                    @Valor, @ValorPago, @Status::status_titulo_financeiro_enum, @CondicaoPagamentoId, @Observacao, @CriadoEm, @CriadoEm)
            RETURNING id;";

        var id = await _session.Connection.ExecuteScalarAsync<int>(sql, Parametros(0, conta), transaction: _session.Transaction);

        return new ContasReceber(id, conta.Descricao, conta.Valor, conta.Cliente, conta.DataVencimento, conta.NumeroDocumento,
            conta.NumeroParcela, conta.TotalParcelas, conta.DataEmissao, conta.CondicaoPagamento, conta.OrigemTipo, conta.OrigemId,
            conta.Observacao, conta.CriadoEm, conta.ValorPago, conta.Status);
    }

    public async Task<ContasReceber> AtualizarContaReceber(int id, ContasReceber conta)
    {
        const string sql = @"
            UPDATE contas_receber
            SET descricao = @Descricao, data_vencimento = @DataVencimento, valor = @Valor, valor_pago = @ValorPago,
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
        var param = new
        {
            Termo = $"%{termo}%",
            filtro?.ClienteId,
            OrigemTipo = filtro?.OrigemTipo?.ToString(),
            filtro?.OrigemId,
            filtro?.NumeroDocumento,
            Status = filtro?.Status?.ToString(),
            TamanhoDaPagina = tamanhoDaPagina,
            Offset = (pagina - 1) * tamanhoDaPagina
        };

        const string where = @"
            WHERE (unaccent(cr.descricao::text) ILIKE unaccent(@Termo::text) OR unaccent(cr.numero_documento::text) ILIKE unaccent(@Termo::text)
                   OR unaccent(c.nome_razaosocial::text) ILIKE unaccent(@Termo::text))
              AND (@ClienteId::int IS NULL OR cr.cliente_id = @ClienteId)
              AND (@OrigemTipo::text IS NULL OR cr.origem_tipo::text = @OrigemTipo)
              AND (@OrigemId::int IS NULL OR cr.origem_id = @OrigemId)
              AND (@NumeroDocumento::text IS NULL OR cr.numero_documento = @NumeroDocumento)
              AND (@Status::text IS NULL OR cr.status::text = @Status)";

        var total = await _session.Connection.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*)
            FROM contas_receber cr
            JOIN clientes c ON c.id = cr.cliente_id" + where + ";",
            param, transaction: _session.Transaction);

        var contas = (await Consultar(where + @"
            ORDER BY cr.data_vencimento, cr.numero_documento, cr.numero_parcela, cr.id
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
        ClienteId = c.Cliente.Id,
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
