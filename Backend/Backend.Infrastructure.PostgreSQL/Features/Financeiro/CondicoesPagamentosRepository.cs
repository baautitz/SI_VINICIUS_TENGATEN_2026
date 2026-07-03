using Backend.Core.Common.Results;
using Backend.Core.Features.Financeiro.Entities;
using Backend.Core.Features.Financeiro.Repositories;
using Backend.Infrastructure.PostgreSQL.Common;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;

namespace Backend.Infrastructure.PostgreSQL.Features.Financeiro;

public class CondicoesPagamentosRepository : ICondicoesPagamentosRepository
{
    private readonly DbSession _session;

    public CondicoesPagamentosRepository(DbSession session)
    {
        _session = session;
    }

    public async Task<ResultadoPaginado<CondicoesPagamentos>> ObterCondicoesPagamentos(int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sqlCount = "SELECT COUNT(*) FROM condicoes_pagamentos;";
        const string sqlData = @"
            SELECT cp.id AS Id, cp.descricao AS Descricao, cp.entrada_minima_percentual AS EntradaMinimaPercentual,
                   cp.desconto_percentual AS DescontoPercentual, cp.acrescimo_percentual AS AcrescimoPercentual,
                   cp.multa_percentual AS MultaPercentual, cp.taxa_juros_percentual AS TaxaJurosPercentual, cp.ativo AS Ativo,
                   mp.codigo AS Codigo, mp.descricao AS Descricao, mp.ativo AS Ativo
            FROM condicoes_pagamentos cp
            INNER JOIN metodos_pagamento mp ON mp.codigo = cp.metodo_pagamento_codigo
            ORDER BY cp.id DESC
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        var total = await _session.Connection.ExecuteScalarAsync<int>(sqlCount, transaction: _session.Transaction);
        
        var condicoes = (await _session.Connection.QueryAsync<CondicoesPagamentos, MetodosPagamentos, CondicoesPagamentos>(
            sqlData,
            (condicao, metodo) =>
            {
                condicao.AtualizarMetodoPagamento(metodo);
                condicao.CarregarParcelas(new List<CondicoesPagamentosParcelas>());
                return condicao;
            },
            new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction,
            splitOn: "Codigo"
        )).ToList();

        if (condicoes.Any())
        {
            var ids = condicoes.Select(c => c.Id).ToArray();
            const string sqlParcelas = @"
                SELECT id AS Id, condicao_pagamento_id AS CondicaoPagamentoId, numero_parcela AS NumeroParcela, 
                       percentual AS Percentual, prazo_dias AS PrazoDias
                FROM condicoes_pagamentos_parcelas
                WHERE condicao_pagamento_id = ANY(@Ids)
                ORDER BY condicao_pagamento_id, numero_parcela;";

            var parcelas = (await _session.Connection.QueryAsync<ParcelaDbRow>(
                sqlParcelas,
                new { Ids = ids },
                transaction: _session.Transaction
            )).ToList();

            var parcelasPorCondicao = parcelas
                .GroupBy(p => p.CondicaoPagamentoId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(p => new CondicoesPagamentosParcelas(p.NumeroParcela, p.Percentual, p.PrazoDias) { Id = p.Id }).ToList()
                );

            foreach (var condicao in condicoes)
            {
                if (parcelasPorCondicao.TryGetValue(condicao.Id, out var subParcelas))
                {
                    condicao.CarregarParcelas(subParcelas);
                }
            }
        }

        return new ResultadoPaginado<CondicoesPagamentos>(condicoes, total, pagina, tamanhoDaPagina);
    }

    public async Task<CondicoesPagamentos?> ObterCondicaoPagamentoPorId(int id)
    {
        const string sqlData = @"
            SELECT cp.id AS Id, cp.descricao AS Descricao, cp.entrada_minima_percentual AS EntradaMinimaPercentual,
                   cp.desconto_percentual AS DescontoPercentual, cp.acrescimo_percentual AS AcrescimoPercentual,
                   cp.multa_percentual AS MultaPercentual, cp.taxa_juros_percentual AS TaxaJurosPercentual, cp.ativo AS Ativo,
                   mp.codigo AS Codigo, mp.descricao AS Descricao, mp.ativo AS Ativo
            FROM condicoes_pagamentos cp
            INNER JOIN metodos_pagamento mp ON mp.codigo = cp.metodo_pagamento_codigo
            WHERE cp.id = @Id;";

        var result = await _session.Connection.QueryAsync<CondicoesPagamentos, MetodosPagamentos, CondicoesPagamentos>(
            sqlData,
            (condicao, metodo) =>
            {
                condicao.AtualizarMetodoPagamento(metodo);
                condicao.CarregarParcelas(new List<CondicoesPagamentosParcelas>());
                return condicao;
            },
            new { Id = id },
            transaction: _session.Transaction,
            splitOn: "Codigo"
        );

        var condicao = result.SingleOrDefault();
        if (condicao is null) return null;

        const string sqlParcelas = @"
            SELECT id AS Id, condicao_pagamento_id AS CondicaoPagamentoId, numero_parcela AS NumeroParcela, percentual AS Percentual, prazo_dias AS PrazoDias
            FROM condicoes_pagamentos_parcelas
            WHERE condicao_pagamento_id = @Id
            ORDER BY numero_parcela;";

        var parcelas = (await _session.Connection.QueryAsync<ParcelaDbRow>(
            sqlParcelas,
            new { Id = id },
            transaction: _session.Transaction
        )).Select(p => new CondicoesPagamentosParcelas(p.NumeroParcela, p.Percentual, p.PrazoDias) { Id = p.Id }).ToList();

        condicao.CarregarParcelas(parcelas);
        return condicao;
    }

    public async Task<CondicoesPagamentos> CriarCondicaoPagamento(CondicoesPagamentos condicao)
    {
        try
        {
            const string sql = @"
                INSERT INTO condicoes_pagamentos (descricao, metodo_pagamento_codigo, entrada_minima_percentual,
                                                 desconto_percentual, acrescimo_percentual, multa_percentual,
                                                 taxa_juros_percentual, ativo)
                VALUES (@Descricao, @MetodoPagamentoCodigo, @EntradaMinimaPercentual,
                        @DescontoPercentual, @AcrescimoPercentual, @MultaPercentual,
                        @TaxaJurosPercentual, @Ativo)
                RETURNING id;";

            var idGerado = await _session.Connection.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    condicao.Descricao,
                    MetodoPagamentoCodigo = condicao.MetodoPagamento.Codigo,
                    condicao.EntradaMinimaPercentual,
                    condicao.DescontoPercentual,
                    condicao.AcrescimoPercentual,
                    condicao.MultaPercentual,
                    condicao.TaxaJurosPercentual,
                    condicao.Ativo
                },
                transaction: _session.Transaction
            );

            condicao.Id = idGerado;

            await InserirParcelas(idGerado, condicao.CondicoesPagamentosParcelas);

            return condicao;
        }
        catch (PostgresException ex)
        {
            throw DbExceptionTranslator.Translate(ex);
        }
    }

    public async Task<CondicoesPagamentos> AtualizarCondicaoPagamento(int id, CondicoesPagamentos condicao)
    {
        try
        {
            const string sql = @"
                UPDATE condicoes_pagamentos
                SET descricao = @Descricao, metodo_pagamento_codigo = @MetodoPagamentoCodigo,
                    entrada_minima_percentual = @EntradaMinimaPercentual, desconto_percentual = @DescontoPercentual,
                    acrescimo_percentual = @AcrescimoPercentual, multa_percentual = @MultaPercentual,
                    taxa_juros_percentual = @TaxaJurosPercentual, ativo = @Ativo
                WHERE id = @Id;";

            await _session.Connection.ExecuteAsync(
                sql,
                new
                {
                    Id = id,
                    condicao.Descricao,
                    MetodoPagamentoCodigo = condicao.MetodoPagamento.Codigo,
                    condicao.EntradaMinimaPercentual,
                    condicao.DescontoPercentual,
                    condicao.AcrescimoPercentual,
                    condicao.MultaPercentual,
                    condicao.TaxaJurosPercentual,
                    condicao.Ativo
                },
                transaction: _session.Transaction
            );

            await _session.Connection.ExecuteAsync(
                "DELETE FROM condicoes_pagamentos_parcelas WHERE condicao_pagamento_id = @Id;",
                new { Id = id },
                transaction: _session.Transaction
            );

            await InserirParcelas(id, condicao.CondicoesPagamentosParcelas);

            condicao.Id = id;
            return condicao;
        }
        catch (PostgresException ex)
        {
            throw DbExceptionTranslator.Translate(ex);
        }
    }

    public async Task<bool> DeletarCondicaoPagamento(int id)
    {
        try
        {
            await _session.Connection.ExecuteAsync(
                "DELETE FROM condicoes_pagamentos_parcelas WHERE condicao_pagamento_id = @Id;",
                new { Id = id },
                transaction: _session.Transaction
            );

            var linhasAfetadas = await _session.Connection.ExecuteAsync(
                "DELETE FROM condicoes_pagamentos WHERE id = @Id;",
                new { Id = id },
                transaction: _session.Transaction
            );

            return linhasAfetadas > 0;
        }
        catch (PostgresException ex)
        {
            throw DbExceptionTranslator.Translate(ex);
        }
    }

    public async Task<ResultadoPaginado<CondicoesPagamentos>> PesquisarCondicoesPagamentos(string termo, int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;
        var queryTermo = $"%{termo}%";

        const string sqlCount = @"
            SELECT COUNT(*) FROM condicoes_pagamentos cp
            INNER JOIN metodos_pagamento mp ON mp.codigo = cp.metodo_pagamento_codigo
            WHERE cp.descricao ILIKE @Termo OR mp.descricao ILIKE @Termo;";

        const string sqlData = @"
            SELECT cp.id AS Id, cp.descricao AS Descricao, cp.entrada_minima_percentual AS EntradaMinimaPercentual,
                   cp.desconto_percentual AS DescontoPercentual, cp.acrescimo_percentual AS AcrescimoPercentual,
                   cp.multa_percentual AS MultaPercentual, cp.taxa_juros_percentual AS TaxaJurosPercentual, cp.ativo AS Ativo,
                   mp.codigo AS Codigo, mp.descricao AS Descricao, mp.ativo AS Ativo
            FROM condicoes_pagamentos cp
            INNER JOIN metodos_pagamento mp ON mp.codigo = cp.metodo_pagamento_codigo
            WHERE cp.descricao ILIKE @Termo OR mp.descricao ILIKE @Termo
            ORDER BY cp.id DESC
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        var total = await _session.Connection.ExecuteScalarAsync<int>(sqlCount, new { Termo = queryTermo }, transaction: _session.Transaction);
        
        var condicoes = (await _session.Connection.QueryAsync<CondicoesPagamentos, MetodosPagamentos, CondicoesPagamentos>(
            sqlData,
            (condicao, metodo) =>
            {
                condicao.AtualizarMetodoPagamento(metodo);
                condicao.CarregarParcelas(new List<CondicoesPagamentosParcelas>());
                return condicao;
            },
            new { Termo = queryTermo, TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction,
            splitOn: "Codigo"
        )).ToList();

        if (condicoes.Any())
        {
            var ids = condicoes.Select(c => c.Id).ToArray();
            const string sqlParcelas = @"
                SELECT id AS Id, condicao_pagamento_id AS CondicaoPagamentoId, numero_parcela AS NumeroParcela, 
                       percentual AS Percentual, prazo_dias AS PrazoDias
                FROM condicoes_pagamentos_parcelas
                WHERE condicao_pagamento_id = ANY(@Ids)
                ORDER BY condicao_pagamento_id, numero_parcela;";

            var parcelas = (await _session.Connection.QueryAsync<ParcelaDbRow>(
                sqlParcelas,
                new { Ids = ids },
                transaction: _session.Transaction
            )).ToList();

            var parcelasPorCondicao = parcelas
                .GroupBy(p => p.CondicaoPagamentoId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(p => new CondicoesPagamentosParcelas(p.NumeroParcela, p.Percentual, p.PrazoDias) { Id = p.Id }).ToList()
                );

            foreach (var condicao in condicoes)
            {
                if (parcelasPorCondicao.TryGetValue(condicao.Id, out var subParcelas))
                {
                    condicao.CarregarParcelas(subParcelas);
                }
            }
        }

        return new ResultadoPaginado<CondicoesPagamentos>(condicoes, total, pagina, tamanhoDaPagina);
    }

    public async Task<bool> ExisteDescricao(string descricao, int? ignorarId = null)
    {
        var sql = "SELECT COUNT(1) FROM condicoes_pagamentos WHERE descricao = @Descricao";
        if (ignorarId.HasValue)
        {
            sql += " AND id != @IgnorarId";
        }

        var count = await _session.Connection.ExecuteScalarAsync<int>(
            sql,
            new { Descricao = descricao, IgnorarId = ignorarId },
            transaction: _session.Transaction
        );

        return count > 0;
    }

    private async Task InserirParcelas(int condicaoId, IEnumerable<CondicoesPagamentosParcelas> parcelas)
    {
        const string sqlInsertParcela = @"
            INSERT INTO condicoes_pagamentos_parcelas (condicao_pagamento_id, numero_parcela, percentual, prazo_dias)
            VALUES (@CondicaoPagamentoId, @NumeroParcela, @Percentual, @PrazoDias);";

        await _session.Connection.ExecuteAsync(
            sqlInsertParcela,
            parcelas.Select(p => new
            {
                CondicaoPagamentoId = condicaoId,
                p.NumeroParcela,
                p.Percentual,
                p.PrazoDias
            }),
            transaction: _session.Transaction
        );
    }

    private class ParcelaDbRow
    {
        public int Id { get; set; }
        public int CondicaoPagamentoId { get; set; }
        public int NumeroParcela { get; set; }
        public decimal Percentual { get; set; }
        public int PrazoDias { get; set; }
    }
}

