using Backend.Core.Common.Results;
using Backend.Core.Features.Pagamentos.Entities;
using Backend.Core.Features.Pagamentos.Repositories;
using Backend.Infrastructure.PostgreSQL.Common;
using Dapper;
using System;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;

namespace Backend.Infrastructure.PostgreSQL.Features.Pagamentos;

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
            SELECT id, codigo, descricao, ativo
            FROM metodos_pagamento
            ORDER BY codigo
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        var total = await _session.Connection.ExecuteScalarAsync<int>(sqlCount, transaction: _session.Transaction);
        var itens = await _session.Connection.QueryAsync<MetodosPagamentos>(sqlData, new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset }, transaction: _session.Transaction);

        return new ResultadoPaginado<MetodosPagamentos>(itens.ToList(), total, pagina, tamanhoDaPagina);
    }

    public async Task<MetodosPagamentos?> ObterMetodoPagamentoPorId(int id)
    {
        const string sql = "SELECT id, codigo, descricao, ativo FROM metodos_pagamento WHERE id = @Id;";
        return await _session.Connection.QuerySingleOrDefaultAsync<MetodosPagamentos>(sql, new { Id = id }, transaction: _session.Transaction);
    }

    public async Task<MetodosPagamentos> CriarMetodoPagamento(MetodosPagamentos metodo)
    {
        try
        {
            const string sql = @"
                INSERT INTO metodos_pagamento (codigo, descricao, ativo)
                VALUES (@Codigo, @Descricao, @Ativo)
                RETURNING id;";

            var idGerado = await _session.Connection.ExecuteScalarAsync<int>(
                sql,
                new { metodo.Codigo, metodo.Descricao, metodo.Ativo },
                transaction: _session.Transaction
            );

            metodo.Id = idGerado;
            return metodo;
        }
        catch (PostgresException ex)
        {
            throw DbExceptionTranslator.Translate(ex);
        }
    }

    public async Task<MetodosPagamentos> AtualizarMetodoPagamento(int id, MetodosPagamentos metodo)
    {
        try
        {
            const string sql = @"
                UPDATE metodos_pagamento
                SET codigo = @Codigo, descricao = @Descricao, ativo = @Ativo
                WHERE id = @Id;";

            var linhasAfetadas = await _session.Connection.ExecuteAsync(
                sql,
                new { Id = id, metodo.Codigo, metodo.Descricao, metodo.Ativo },
                transaction: _session.Transaction
            );

            if (linhasAfetadas == 0)
                throw new Exception($"Falha ao atualizar: O método de pagamento com ID {id} não foi encontrado.");

            metodo.Id = id;
            return metodo;
        }
        catch (PostgresException ex)
        {
            throw DbExceptionTranslator.Translate(ex);
        }
    }

    public async Task<bool> DeletarMetodoPagamento(int id)
    {
        try
        {
            const string sql = "DELETE FROM metodos_pagamento WHERE id = @Id;";
            var linhasAfetadas = await _session.Connection.ExecuteAsync(sql, new { Id = id }, transaction: _session.Transaction);
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
            SELECT id, codigo, descricao, ativo
            FROM metodos_pagamento
            WHERE codigo ILIKE @Termo OR descricao ILIKE @Termo
            ORDER BY codigo
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        var total = await _session.Connection.ExecuteScalarAsync<int>(sqlCount, new { Termo = queryTermo }, transaction: _session.Transaction);
        var itens = await _session.Connection.QueryAsync<MetodosPagamentos>(sqlData, new { Termo = queryTermo, TamanhoDaPagina = tamanhoDaPagina, Offset = offset }, transaction: _session.Transaction);

        return new ResultadoPaginado<MetodosPagamentos>(itens.ToList(), total, pagina, tamanhoDaPagina);
    }

    public async Task<bool> ExisteCodigo(string codigo, int? ignorarId = null)
    {
        var sql = "SELECT COUNT(1) FROM metodos_pagamento WHERE codigo = @Codigo";
        if (ignorarId.HasValue)
        {
            sql += " AND id != @IgnorarId";
        }

        var count = await _session.Connection.ExecuteScalarAsync<int>(
            sql,
            new { Codigo = codigo, IgnorarId = ignorarId },
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
            .Select(c => c)
            .FirstOrDefault();
    }
}
