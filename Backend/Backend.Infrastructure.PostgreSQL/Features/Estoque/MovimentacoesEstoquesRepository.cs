using Backend.Core.Common;
using Backend.Core.Features.Acesso.Entities;
using Backend.Core.Features.Catalogo.Entities;
using Backend.Core.Features.Estoque.DTOs;
using Backend.Core.Features.Estoque.Entities;
using Backend.Core.Features.Estoque.Entities.Enums;
using Backend.Core.Features.Estoque.Repositories;
using Backend.Infrastructure.PostgreSQL.Common;
using Dapper;

namespace Backend.Infrastructure.PostgreSQL.Features.Estoque;

public class MovimentacoesEstoquesRepository : IMovimentacoesEstoquesRepository
{
    private readonly DbSession _session;

    public MovimentacoesEstoquesRepository(DbSession session)
    {
        _session = session;
    }

    public async Task<ResultadoPaginado<MovimentacoesEstoques>> ObterMovimentacoes(int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sql = @"
            SELECT COUNT(*) FROM movimentacoes_estoque;

            SELECT me.id, me.data_movimentacao, me.tipo_movimentacao, me.observacao,
                   u.id, u.nome, u.cpf_cnpj, u.email, u.telefone, u.usuario, u.senha, u.ativo
            FROM movimentacoes_estoque me
            LEFT JOIN usuarios u ON u.id = me.usuario_id
            ORDER BY me.data_movimentacao DESC
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql, new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction);

        var total = await multi.ReadSingleAsync<int>();
        var rawMovimentacoes = (await multi.ReadAsync<MovimentacoesEstoques>()).ToList();
        multi.Dispose();

        var movimentacoes = new List<MovimentacoesEstoques>();
        if (rawMovimentacoes.Count > 0)
        {
            var rawIds = rawMovimentacoes.Select(m => m.Id).ToArray();

            const string usuariosSql = @"
                SELECT me.id, u.id, u.nome, u.cpf_cnpj, u.email, u.telefone, u.usuario, u.senha, u.ativo
                FROM movimentacoes_estoque me
                LEFT JOIN usuarios u ON u.id = me.usuario_id
                WHERE me.id = ANY(@Ids);";

            var usuariosPorMovimentacao = (await _session.Connection.QueryAsync<int, Usuarios, (int Id, Usuarios? Usuario)>(
                usuariosSql,
                (meId, usuario) =>
                {
                    if (usuario is not null) usuario.Sessoes = [];
                    return (meId, usuario is not null && usuario.Id > 0 ? usuario : null);
                },
                new { Ids = rawIds },
                transaction: _session.Transaction,
                splitOn: "id"))
                .ToDictionary(x => x.Id, x => x.Usuario);

            foreach (var m in rawMovimentacoes)
            {
                if (usuariosPorMovimentacao.TryGetValue(m.Id, out var u)) m.Usuario = u;
                m.MovimentacoesEstoquesItens = [];
                movimentacoes.Add(m);
            }
        }

        if (movimentacoes.Count > 0)
        {
            var ids = movimentacoes.Select(m => m.Id).ToArray();

            const string itensSql = @"
                SELECT mei.id, mei.quantidade, mei.custo_unitario, mei.movimentacao_estoque_id AS MovimentacaoId,
                       s.sku, s.gtin_ean, s.preco, s.estoque, s.ativo
                FROM movimentacoes_estoque_itens mei
                JOIN skus s ON s.sku = mei.sku
                WHERE mei.movimentacao_estoque_id = ANY(@Ids);";

            var itens = (await _session.Connection.QueryAsync<MovimentacoesEstoquesItens, Skus, MovimentacoesEstoquesItens>(
                itensSql,
                (item, sku) =>
                {
                    sku.SkusAtributosValores = [];
                    item.Sku = sku;
                    return item;
                },
                new { Ids = ids },
                transaction: _session.Transaction,
                splitOn: "sku")).ToList();

            var itensPorMovimentacao = itens
                .GroupBy(i => i.MovimentacaoId)
                .ToDictionary(g => g.Key, g => g.AsEnumerable());

            foreach (var m in movimentacoes)
            {
                if (itensPorMovimentacao.TryGetValue(m.Id, out var sub))
                    m.MovimentacoesEstoquesItens = sub;
            }
        }

        return new ResultadoPaginado<MovimentacoesEstoques>(movimentacoes, total, pagina, tamanhoDaPagina);
    }

    public async Task<MovimentacoesEstoques?> ObterMovimentacaoPorId(int id)
    {
        const string movimentacaoSql = @"
            SELECT me.id, me.data_movimentacao, me.tipo_movimentacao, me.observacao,
                   u.id, u.nome, u.cpf_cnpj, u.email, u.telefone, u.usuario, u.senha, u.ativo
            FROM movimentacoes_estoque me
            LEFT JOIN usuarios u ON u.id = me.usuario_id
            WHERE me.id = @Id;";

        const string itensSql = @"
            SELECT mei.id, mei.quantidade, mei.custo_unitario,
                   s.sku, s.gtin_ean, s.preco, s.estoque, s.ativo
            FROM movimentacoes_estoque_itens mei
            JOIN skus s ON s.sku = mei.sku
            WHERE mei.movimentacao_estoque_id = @Id;";

        var movimentacao = (await _session.Connection.QueryAsync<MovimentacoesEstoques, Usuarios, MovimentacoesEstoques>(
            movimentacaoSql,
            (m, usuario) =>
            {
                if (usuario is not null) { usuario.Sessoes = []; m.Usuario = usuario; }
                m.MovimentacoesEstoquesItens = [];
                return m;
            },
            new { Id = id },
            transaction: _session.Transaction,
            splitOn: "id")).SingleOrDefault();

        if (movimentacao is null) return null;

        var itens = await _session.Connection.QueryAsync<MovimentacoesEstoquesItens, Skus, MovimentacoesEstoquesItens>(
            itensSql,
            (item, sku) =>
            {
                sku.SkusAtributosValores = [];
                item.Sku = sku;
                return item;
            },
            new { Id = id },
            transaction: _session.Transaction,
            splitOn: "sku");

        movimentacao.MovimentacoesEstoquesItens = itens;
        return movimentacao;
    }

    public async Task<MovimentacoesEstoques> CriarMovimentacao(MovimentacoesEstoques movimentacao)
    {
        const string sql = @"
            INSERT INTO movimentacoes_estoque (data_movimentacao, tipo_movimentacao, observacao, usuario_id, nfe_id)
            VALUES (@DataMovimentacao, @TipoMovimentacao, @Observacao, @UsuarioId, @NfeId)
            RETURNING id;";

        var idGerado = await _session.Connection.ExecuteScalarAsync<int>(
            sql,
            new
            {
                movimentacao.DataMovimentacao,
                movimentacao.TipoMovimentacao,
                movimentacao.Observacao,
                UsuarioId = movimentacao.Usuario?.Id,
                NfeId = movimentacao.Nfe?.Id
            },
            transaction: _session.Transaction);

        movimentacao.Id = idGerado;

        await InserirItens(idGerado, movimentacao.MovimentacoesEstoquesItens);
        await AtualizarEstoqueSkus(movimentacao.MovimentacoesEstoquesItens, movimentacao.TipoMovimentacao);

        return movimentacao;
    }

    public async Task<MovimentacoesEstoques> AtualizarMovimentacao(int id, MovimentacoesEstoques movimentacao)
    {
        var antiga = await ObterMovimentacaoPorId(id);

        const string sql = @"
            UPDATE movimentacoes_estoque
            SET data_movimentacao = @DataMovimentacao,
                tipo_movimentacao = @TipoMovimentacao,
                observacao = @Observacao,
                usuario_id = @UsuarioId,
                nfe_id = @NfeId
            WHERE id = @Id;";

        await _session.Connection.ExecuteAsync(
            sql,
            new
            {
                Id = id,
                movimentacao.DataMovimentacao,
                movimentacao.TipoMovimentacao,
                movimentacao.Observacao,
                UsuarioId = movimentacao.Usuario?.Id,
                NfeId = movimentacao.Nfe?.Id
            },
            transaction: _session.Transaction);

        if (antiga is not null)
            await EstornarEstoqueSkus(antiga.MovimentacoesEstoquesItens, antiga.TipoMovimentacao);

        await ReplacerItens(id, movimentacao.MovimentacoesEstoquesItens);
        await AtualizarEstoqueSkus(movimentacao.MovimentacoesEstoquesItens, movimentacao.TipoMovimentacao);

        movimentacao.Id = id;
        return movimentacao;
    }

    public async Task<bool> DeletarMovimentacao(int id)
    {
        var movimentacao = await ObterMovimentacaoPorId(id);
        if (movimentacao is not null)
            await EstornarEstoqueSkus(movimentacao.MovimentacoesEstoquesItens, movimentacao.TipoMovimentacao);

        await _session.Connection.ExecuteAsync(
            "DELETE FROM movimentacoes_estoque_itens WHERE movimentacao_estoque_id = @Id;",
            new { Id = id }, transaction: _session.Transaction);

        var linhasAfetadas = await _session.Connection.ExecuteAsync(
            "DELETE FROM movimentacoes_estoque WHERE id = @Id;",
            new { Id = id }, transaction: _session.Transaction);

        return linhasAfetadas > 0;
    }

    public async Task<ResultadoPaginado<MovimentacoesEstoquesResumo>> ObterMovimentacoesResumo(int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sql = @"
            SELECT COUNT(*) FROM movimentacoes_estoque;

            SELECT id, data_movimentacao, tipo_movimentacao, observacao
            FROM movimentacoes_estoque
            ORDER BY data_movimentacao DESC
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql, new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction);

        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<MovimentacoesEstoquesResumo>();

        return new ResultadoPaginado<MovimentacoesEstoquesResumo>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<ResultadoPaginado<MovimentacoesEstoquesResumo>> PesquisarMovimentacoes(string termo, int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sql = @"
            SELECT COUNT(*)
            FROM movimentacoes_estoque
            WHERE observacao ILIKE @Termo OR tipo_movimentacao::text ILIKE @Termo;

            SELECT id, data_movimentacao, tipo_movimentacao, observacao
            FROM movimentacoes_estoque
            WHERE observacao ILIKE @Termo OR tipo_movimentacao::text ILIKE @Termo
            ORDER BY data_movimentacao DESC
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql,
            new { Termo = $"%{termo}%", TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction);

        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<MovimentacoesEstoquesResumo>();

        return new ResultadoPaginado<MovimentacoesEstoquesResumo>(itens, total, pagina, tamanhoDaPagina);
    }

    private async Task InserirItens(int movimentacaoId, IEnumerable<MovimentacoesEstoquesItens> itens)
    {
        const string sql = @"
            INSERT INTO movimentacoes_estoque_itens (quantidade, custo_unitario, sku, movimentacao_estoque_id)
            VALUES (@Quantidade, @CustoUnitario, @SkuCodigo, @MovimentacaoId);";

        await _session.Connection.ExecuteAsync(
            sql,
            itens.Select(i => new
            {
                i.Quantidade,
                i.CustoUnitario,
                SkuCodigo = i.Sku.Sku,
                MovimentacaoId = movimentacaoId
            }),
            transaction: _session.Transaction);
    }

    private async Task ReplacerItens(int movimentacaoId, IEnumerable<MovimentacoesEstoquesItens> itens)
    {
        await _session.Connection.ExecuteAsync(
            "DELETE FROM movimentacoes_estoque_itens WHERE movimentacao_estoque_id = @MovimentacaoId;",
            new { MovimentacaoId = movimentacaoId },
            transaction: _session.Transaction);

        await InserirItens(movimentacaoId, itens);
    }

    private async Task AtualizarEstoqueSkus(IEnumerable<MovimentacoesEstoquesItens> itens, TipoMovimentacaoEstoque tipo)
    {
        var sinal = tipo == TipoMovimentacaoEstoque.ENTRADA ? 1 : -1;

        const string sql = "UPDATE skus SET estoque = estoque + (@Sinal * @Quantidade) WHERE sku = @SkuCodigo;";

        await _session.Connection.ExecuteAsync(
            sql,
            itens.Select(i => new { Sinal = sinal, i.Quantidade, SkuCodigo = i.Sku.Sku }),
            transaction: _session.Transaction);
    }

    private Task EstornarEstoqueSkus(IEnumerable<MovimentacoesEstoquesItens> itens, TipoMovimentacaoEstoque tipo)
    {
        var tipoInverso = tipo == TipoMovimentacaoEstoque.ENTRADA
            ? TipoMovimentacaoEstoque.SAIDA
            : TipoMovimentacaoEstoque.ENTRADA;

        return AtualizarEstoqueSkus(itens, tipoInverso);
    }
}
