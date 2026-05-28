using System.Linq;
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
                   u.id AS UsuarioId, u.nome AS UsuarioNome, u.cpf_cnpj AS UsuarioCpfCnpj, u.email AS UsuarioEmail,
                   u.telefone AS UsuarioTelefone, u.usuario AS UsuarioUsuario, u.senha AS UsuarioSenha, u.ativo AS UsuarioAtivo
            FROM movimentacoes_estoque me
            LEFT JOIN usuarios u ON u.id = me.usuario_id
            ORDER BY me.data_movimentacao DESC
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql, new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction);

        var total = await multi.ReadSingleAsync<int>();
        var movimentacoesDto = (await multi.ReadAsync<MovimentacaoDto>()).ToList();

        if (!movimentacoesDto.Any())
        {
            return new ResultadoPaginado<MovimentacoesEstoques>(Enumerable.Empty<MovimentacoesEstoques>(), total, pagina, tamanhoDaPagina);
        }

        var ids = movimentacoesDto.Select(m => m.Id).ToArray();

        const string itensSql = @"
            SELECT mei.id, mei.quantidade, mei.custo_unitario, mei.movimentacao_estoque_id AS MovimentacaoId,
                   s.sku AS SkuCodigo, s.gtin_ean AS SkuGtinEan, s.preco AS SkuPreco, s.estoque AS SkuEstoque, s.ativo AS SkuAtivo
            FROM movimentacoes_estoque_itens mei
            JOIN skus s ON s.sku = mei.sku
            WHERE mei.movimentacao_estoque_id = ANY(@Ids);";

        var itensDto = (await _session.Connection.QueryAsync<MovimentacaoItemDto>(
            itensSql,
            new { Ids = ids },
            transaction: _session.Transaction)).ToList();

        var itensPorMovimentacao = itensDto
            .GroupBy(i => i.MovimentacaoId)
            .ToDictionary(g => g.Key, g => g.AsEnumerable());

        var movimentacoes = movimentacoesDto.Select(dto =>
        {
            var usuario = dto.UsuarioId.HasValue ? BuildUsuario(new UsuarioDto(dto.UsuarioId.Value, dto.UsuarioNome ?? string.Empty, dto.UsuarioCpfCnpj ?? string.Empty, dto.UsuarioEmail ?? string.Empty, dto.UsuarioTelefone ?? string.Empty, dto.UsuarioUsuario ?? string.Empty, dto.UsuarioSenha ?? string.Empty, dto.UsuarioAtivo ?? false)) : null;
            var movimentacao = new MovimentacoesEstoques(dto.Id, dto.DataMovimentacao, dto.TipoMovimentacao, usuario, null, dto.Observacao);

            if (itensPorMovimentacao.TryGetValue(dto.Id, out var itens))
            {
                foreach (var itemDto in itens)
                {
                    movimentacao.AdicionarItemExistente(BuildItem(itemDto, dto.Id));
                }
            }

            return movimentacao;
        }).ToList();

        return new ResultadoPaginado<MovimentacoesEstoques>(movimentacoes, total, pagina, tamanhoDaPagina);
    }

    public async Task<MovimentacoesEstoques?> ObterMovimentacaoPorId(int id)
    {
        const string movimentacaoSql = @"
            SELECT me.id, me.data_movimentacao, me.tipo_movimentacao, me.observacao,
                   u.id AS UsuarioId, u.nome AS UsuarioNome, u.cpf_cnpj AS UsuarioCpfCnpj, u.email AS UsuarioEmail,
                   u.telefone AS UsuarioTelefone, u.usuario AS UsuarioUsuario, u.senha AS UsuarioSenha, u.ativo AS UsuarioAtivo
            FROM movimentacoes_estoque me
            LEFT JOIN usuarios u ON u.id = me.usuario_id
            WHERE me.id = @Id;";

        const string itensSql = @"
            SELECT mei.id, mei.quantidade, mei.custo_unitario, mei.movimentacao_estoque_id AS MovimentacaoId,
                   s.sku AS SkuCodigo, s.gtin_ean AS SkuGtinEan, s.preco AS SkuPreco, s.estoque AS SkuEstoque, s.ativo AS SkuAtivo
            FROM movimentacoes_estoque_itens mei
            JOIN skus s ON s.sku = mei.sku
            WHERE mei.movimentacao_estoque_id = @Id;";

        var dto = await _session.Connection.QuerySingleOrDefaultAsync<MovimentacaoDto>(
            movimentacaoSql,
            new { Id = id },
            transaction: _session.Transaction);

        if (dto is null) return null;

        var usuario = dto.UsuarioId.HasValue ? BuildUsuario(new UsuarioDto(dto.UsuarioId.Value, dto.UsuarioNome ?? string.Empty, dto.UsuarioCpfCnpj ?? string.Empty, dto.UsuarioEmail ?? string.Empty, dto.UsuarioTelefone ?? string.Empty, dto.UsuarioUsuario ?? string.Empty, dto.UsuarioSenha ?? string.Empty, dto.UsuarioAtivo ?? false)) : null;
        var movimentacao = new MovimentacoesEstoques(dto.Id, dto.DataMovimentacao, dto.TipoMovimentacao, usuario, null, dto.Observacao);

        var itensDto = await _session.Connection.QueryAsync<MovimentacaoItemDto>(
            itensSql,
            new { Id = id },
            transaction: _session.Transaction);

        foreach (var itemDto in itensDto)
        {
            movimentacao.AdicionarItemExistente(BuildItem(itemDto, dto.Id));
        }

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

        await InserirItens(idGerado, movimentacao.MovimentacoesEstoquesItens);
        await AtualizarEstoqueSkus(movimentacao.MovimentacoesEstoquesItens, movimentacao.TipoMovimentacao);

        var persisted = new MovimentacoesEstoques(idGerado, movimentacao.DataMovimentacao, movimentacao.TipoMovimentacao, movimentacao.Usuario, movimentacao.Nfe, movimentacao.Observacao);
        foreach (var item in movimentacao.MovimentacoesEstoquesItens)
        {
            persisted.AdicionarItemExistente(new MovimentacoesEstoquesItens(item.Id, idGerado, item.Sku, item.Quantidade, item.CustoUnitario));
        }

        return persisted;
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

        var updated = new MovimentacoesEstoques(id, movimentacao.DataMovimentacao, movimentacao.TipoMovimentacao, movimentacao.Usuario, movimentacao.Nfe, movimentacao.Observacao);
        foreach (var item in movimentacao.MovimentacoesEstoquesItens)
        {
            updated.AdicionarItemExistente(new MovimentacoesEstoquesItens(item.Id, id, item.Sku, item.Quantidade, item.CustoUnitario));
        }

        return updated;
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

    private static MovimentacoesEstoquesItens BuildItem(MovimentacaoItemDto dto, int movimentacaoId)
    {
        var sku = new Skus(dto.SkuCodigo, dto.SkuPreco, dto.SkuEstoque, dto.SkuGtinEan);
        if (!dto.SkuAtivo)
            sku.Desativar();

        return new MovimentacoesEstoquesItens(dto.Id, movimentacaoId, sku, dto.Quantidade, dto.CustoUnitario);
    }

    private static Usuarios BuildUsuario(UsuarioDto dto)
    {
        return new Usuarios(dto.Id, dto.Nome, dto.CpfCnpj, dto.Email, dto.Usuario, dto.Senha, dto.Telefone, dto.Ativo);
    }

    private sealed record MovimentacaoDto(int Id, DateTime DataMovimentacao, TipoMovimentacaoEstoque TipoMovimentacao, string? Observacao,
        int? UsuarioId, string? UsuarioNome, string? UsuarioCpfCnpj, string? UsuarioEmail, string? UsuarioTelefone,
        string? UsuarioUsuario, string? UsuarioSenha, bool? UsuarioAtivo);

    private sealed record MovimentacaoItemDto(int Id, decimal Quantidade, decimal CustoUnitario, int MovimentacaoId,
        string SkuCodigo, string SkuGtinEan, decimal SkuPreco, decimal SkuEstoque, bool SkuAtivo);

    private sealed record UsuarioDto(int Id, string Nome, string CpfCnpj, string Email, string Telefone,
        string Usuario, string Senha, bool Ativo);
}
