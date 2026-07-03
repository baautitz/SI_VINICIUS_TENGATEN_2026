using Backend.Core.Common.Results;
using Backend.Core.Features.Financeiro.Entities;
using Backend.Core.Features.Financeiro.Repositories;
using Backend.Infrastructure.PostgreSQL.Common;
using Dapper;
using System;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;

namespace Backend.Infrastructure.PostgreSQL.Features.Financeiro;

public class MetodosPagamentosRepository : IMetodosPagamentosRepository
{
    private readonly DbSession _session;

    public MetodosPagamentosRepository(DbSession session)
    {
        _session = session;
    }

    public async Task<ResultadoPaginado<MetodosPagamentos>> ObterMetodosPagamentos(int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sqlCount = "SELECT COUNT(*) FROM metodos_pagamento;";
        const string sqlData = @"
            SELECT codigo, descricao, ativo
            FROM metodos_pagamento
            ORDER BY codigo DESC
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        var total = await _session.Connection.ExecuteScalarAsync<int>(sqlCount, transaction: _session.Transaction);
        var itens = await _session.Connection.QueryAsync<MetodosPagamentos>(sqlData, new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset }, transaction: _session.Transaction);

        return new ResultadoPaginado<MetodosPagamentos>(itens.ToList(), total, pagina, tamanhoDaPagina);
    }

    public async Task<MetodosPagamentos?> ObterMetodoPagamentoPorCodigo(string codigo)
    {
        const string sql = "SELECT codigo, descricao, ativo FROM metodos_pagamento WHERE codigo = @Codigo;";
        return await _session.Connection.QuerySingleOrDefaultAsync<MetodosPagamentos>(sql, new { Codigo = codigo }, transaction: _session.Transaction);
    }

    public async Task<MetodosPagamentos> CriarMetodoPagamento(MetodosPagamentos metodo)
    {
        try
        {
            const string sql = @"
                INSERT INTO metodos_pagamento (codigo, descricao, ativo)
                VALUES (@Codigo, @Descricao, @Ativo);";

            await _session.Connection.ExecuteAsync(
                sql,
                new { metodo.Codigo, metodo.Descricao, metodo.Ativo },
                transaction: _session.Transaction
            );

            return metodo;
        }
        catch (PostgresException ex)
        {
            throw DbExceptionTranslator.Translate(ex);
        }
    }

    public async Task<MetodosPagamentos> AtualizarMetodoPagamento(string codigo, MetodosPagamentos metodo)
    {
        try
        {
            const string sql = @"
                UPDATE metodos_pagamento
                SET descricao = @Descricao, ativo = @Ativo
                WHERE codigo = @Codigo;";

            var linhasAfetadas = await _session.Connection.ExecuteAsync(
                sql,
                new { Codigo = codigo, metodo.Descricao, metodo.Ativo },
                transaction: _session.Transaction
            );

            if (linhasAfetadas == 0)
                throw new Exception($"Falha ao atualizar: o método de pagamento com código '{codigo}' não foi encontrado.");

            return metodo;
        }
        catch (PostgresException ex)
        {
            throw DbExceptionTranslator.Translate(ex);
        }
    }

    public async Task<bool> DeletarMetodoPagamento(string codigo)
    {
        try
        {
            const string sql = "DELETE FROM metodos_pagamento WHERE codigo = @Codigo;";
            var linhasAfetadas = await _session.Connection.ExecuteAsync(sql, new { Codigo = codigo }, transaction: _session.Transaction);
            return linhasAfetadas > 0;
        }
        catch (PostgresException ex)
        {
            throw DbExceptionTranslator.Translate(ex);
        }
    }

    public async Task<ResultadoPaginado<MetodosPagamentos>> PesquisarMetodosPagamentos(string termo, int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;
        var queryTermo = $"%{termo}%";

        const string sqlCount = "SELECT COUNT(*) FROM metodos_pagamento WHERE codigo ILIKE @Termo OR descricao ILIKE @Termo;";
        const string sqlData = @"
            SELECT codigo, descricao, ativo
            FROM metodos_pagamento
            WHERE codigo ILIKE @Termo OR descricao ILIKE @Termo
            ORDER BY codigo DESC
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        var total = await _session.Connection.ExecuteScalarAsync<int>(sqlCount, new { Termo = queryTermo }, transaction: _session.Transaction);
        var itens = await _session.Connection.QueryAsync<MetodosPagamentos>(sqlData, new { Termo = queryTermo, TamanhoDaPagina = tamanhoDaPagina, Offset = offset }, transaction: _session.Transaction);

        return new ResultadoPaginado<MetodosPagamentos>(itens.ToList(), total, pagina, tamanhoDaPagina);
    }

    public async Task<bool> ExisteCodigo(string codigo, string? ignorarCodigo = null)
    {
        var sql = "SELECT COUNT(1) FROM metodos_pagamento WHERE codigo = @Codigo";
        if (ignorarCodigo is not null)
        {
            sql += " AND codigo != @IgnorarCodigo";
        }

        var count = await _session.Connection.ExecuteScalarAsync<int>(
            sql,
            new { Codigo = codigo, IgnorarCodigo = ignorarCodigo },
            transaction: _session.Transaction
        );

        return count > 0;
    }

    public async Task<string?> ObterUltimoCodigo()
    {
        const string sql = "SELECT codigo FROM metodos_pagamento;";
        var codigos = await _session.Connection.QueryAsync<string>(sql, transaction: _session.Transaction);
        return codigos
            .Where(c => int.TryParse(c, out _))
            .OrderByDescending(c => int.Parse(c))
            .FirstOrDefault();
    }
}

