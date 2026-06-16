using System.Linq;
using Backend.Core.Common.Results;
using Backend.Core.Features.Acesso.Entities;
using Backend.Core.Features.Catalogo.Entities;
using Backend.Core.Features.Estoque.Entities;
using Backend.Core.Features.Estoque.Entities.Enums;
using Backend.Core.Features.Estoque.Repositories;
using Backend.Core.Features.Vendas.Entities;
using Backend.Infrastructure.PostgreSQL.Common;
using Dapper;
using Npgsql;

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

            SELECT me.id, me.data_movimentacao, me.tipo_movimentacao, me.status, me.observacao,
                   u.id AS UsuarioId, u.nome AS UsuarioNome, u.cpf_cnpj AS UsuarioCpfCnpj, u.email AS UsuarioEmail,
                   u.telefone AS UsuarioTelefone, u.usuario AS UsuarioUsuario, u.senha AS UsuarioSenha, u.ativo AS UsuarioAtivo,
                   me.venda_id AS VendaId
            FROM movimentacoes_estoque me
            LEFT JOIN usuarios u ON u.id = me.usuario_id
            ORDER BY me.data_movimentacao DESC
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql, new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction);

        var total = await multi.ReadSingleAsync<int>();
        var movimentacoesDbRow = (await multi.ReadAsync<MovimentacaoDbRow>()).ToList();

        if (!movimentacoesDbRow.Any())
        {
            return new ResultadoPaginado<MovimentacoesEstoques>(Enumerable.Empty<MovimentacoesEstoques>(), total, pagina, tamanhoDaPagina);
        }

        var ids = movimentacoesDbRow.Select(m => m.Id).ToArray();

        const string itensSql = @"
            SELECT mei.id, mei.quantidade, mei.custo_unitario, mei.movimentacao_estoque_id AS MovimentacaoId,
                   s.sku AS SkuCodigo, s.gtin_ean AS SkuGtinEan, s.preco AS SkuPreco, s.estoque AS SkuEstoque, s.ativo AS SkuAtivo,
                   s.custo_medio AS SkuCustoMedio, s.custo_ultima_compra AS SkuCustoUltimaCompra,
                   mei.quantidade_anterior AS QuantidadeAnterior, mei.custo_medio_anterior AS CustoMedioAnterior,
                   p.id, p.produto, p.descricao, p.ativo,
                   c.id, c.categoria, c.descricao, c.ativo,
                   m.id, m.marca, m.descricao, m.ativo,
                   u.id, u.sigla, u.descricao, u.categoria, u.permite_decimais AS PermiteDecimais, u.ativo
            FROM movimentacoes_estoque_itens mei
            JOIN skus s ON s.sku = mei.sku
            JOIN produtos p ON p.id = s.produto_id
            JOIN categorias c ON c.id = p.categoria_id
            JOIN marcas m ON m.id = p.marca_id
            JOIN unidades_medida u ON u.id = p.unidade_medida_id
            WHERE mei.movimentacao_estoque_id = ANY(@Ids);";

        var itensDbRow = (await _session.Connection.QueryAsync<MovimentacaoItemDbRow, Produtos, Categorias, Marcas, UnidadesMedida, MovimentacaoItemDbRow>(
            itensSql,
            (itemDbRow, produto, categoria, marca, unidadeMedida) =>
            {
                itemDbRow.Produto = new Produtos(produto.Id, produto.Produto, produto.Descricao, categoria, marca, unidadeMedida);
                return itemDbRow;
            },
            new { Ids = ids },
            splitOn: "id,id,id,id",
            transaction: _session.Transaction)).ToList();

        const string atributosSql = @"
            SELECT savr.sku AS Sku, sav.chave_id AS ChaveId, sav.valor AS Valor,
                   sav.id AS Id, sak.chave AS Chave
            FROM skus_atributos_valores_relacionamento savr
            JOIN sku_atributos_valores sav ON sav.id = savr.valor_id
            JOIN sku_atributos_chaves sak ON sak.id = sav.chave_id
            WHERE savr.sku IN (SELECT sku FROM movimentacoes_estoque_itens WHERE movimentacao_estoque_id = ANY(@Ids));";

        var atributosDbRow = (await _session.Connection.QueryAsync<AtributoDbRow>(
            atributosSql, new { Ids = ids }, transaction: _session.Transaction)).ToList();
        
        var atributosPorSku = atributosDbRow.GroupBy(a => a.Sku).ToDictionary(g => g.Key, g => g.AsEnumerable());

        var itensPorMovimentacao = itensDbRow
            .GroupBy(i => i.MovimentacaoId)
            .ToDictionary(g => g.Key, g => g.AsEnumerable());

        var movimentacoes = movimentacoesDbRow.Select(row =>
        {
            var usuario = row.UsuarioId.HasValue ? BuildUsuario(new UsuarioDbRow(row.UsuarioId.Value, row.UsuarioNome ?? string.Empty, row.UsuarioCpfCnpj ?? string.Empty, row.UsuarioEmail ?? string.Empty, row.UsuarioTelefone ?? string.Empty, row.UsuarioUsuario ?? string.Empty, row.UsuarioSenha ?? string.Empty, row.UsuarioAtivo ?? false)) : null;
            var venda = row.VendaId.HasValue ? new Venda(row.VendaId.Value) : null;
            var movimentacao = new MovimentacoesEstoques(row.Id, row.DataMovimentacao, row.TipoMovimentacao, usuario, null, venda, row.Observacao, row.Status);

            if (itensPorMovimentacao.TryGetValue(row.Id, out var itens))
            {
                foreach (var itemDbRow in itens)
                {
                    movimentacao.AdicionarItemExistente(BuildItem(itemDbRow, row.Id, atributosPorSku.GetValueOrDefault(itemDbRow.SkuCodigo, Enumerable.Empty<AtributoDbRow>())));
                }
            }

            return movimentacao;
        }).ToList();

        return new ResultadoPaginado<MovimentacoesEstoques>(movimentacoes, total, pagina, tamanhoDaPagina);
    }

    public async Task<MovimentacoesEstoques?> ObterMovimentacaoPorId(int id)
    {
        const string movimentacaoSql = @"
            SELECT me.id, me.data_movimentacao, me.tipo_movimentacao, me.status, me.observacao,
                   u.id AS UsuarioId, u.nome AS UsuarioNome, u.cpf_cnpj AS UsuarioCpfCnpj, u.email AS UsuarioEmail,
                   u.telefone AS UsuarioTelefone, u.usuario AS UsuarioUsuario, u.senha AS UsuarioSenha, u.ativo AS UsuarioAtivo,
                   me.venda_id AS VendaId
            FROM movimentacoes_estoque me
            LEFT JOIN usuarios u ON u.id = me.usuario_id
            WHERE me.id = @Id;";

        const string itensSql = @"
            SELECT mei.id, mei.quantidade, mei.custo_unitario, mei.movimentacao_estoque_id AS MovimentacaoId,
                   s.sku AS SkuCodigo, s.gtin_ean AS SkuGtinEan, s.preco AS SkuPreco, s.estoque AS SkuEstoque, s.ativo AS SkuAtivo,
                   s.custo_medio AS SkuCustoMedio, s.custo_ultima_compra AS SkuCustoUltimaCompra,
                   mei.quantidade_anterior AS QuantidadeAnterior, mei.custo_medio_anterior AS CustoMedioAnterior,
                   p.id, p.produto, p.descricao, p.ativo,
                   c.id, c.categoria, c.descricao, c.ativo,
                   m.id, m.marca, m.descricao, m.ativo,
                   u.id, u.sigla, u.descricao, u.categoria, u.permite_decimais AS PermiteDecimais, u.ativo
            FROM movimentacoes_estoque_itens mei
            JOIN skus s ON s.sku = mei.sku
            JOIN produtos p ON p.id = s.produto_id
            JOIN categorias c ON c.id = p.categoria_id
            JOIN marcas m ON m.id = p.marca_id
            JOIN unidades_medida u ON u.id = p.unidade_medida_id
            WHERE mei.movimentacao_estoque_id = @Id
            ORDER BY mei.id ASC;

            SELECT savr.sku AS Sku, sav.chave_id AS ChaveId, sav.valor AS Valor,
                   sav.id AS Id, sak.chave AS Chave
            FROM skus_atributos_valores_relacionamento savr
            JOIN sku_atributos_valores sav ON sav.id = savr.valor_id
            JOIN sku_atributos_chaves sak ON sak.id = sav.chave_id
            WHERE savr.sku IN (SELECT sku FROM movimentacoes_estoque_itens WHERE movimentacao_estoque_id = @Id);";

        var row = await _session.Connection.QuerySingleOrDefaultAsync<MovimentacaoDbRow>(
            movimentacaoSql,
            new { Id = id },
            transaction: _session.Transaction);

        if (row is null) return null;

        var usuario = row.UsuarioId.HasValue ? BuildUsuario(new UsuarioDbRow(row.UsuarioId.Value, row.UsuarioNome ?? string.Empty, row.UsuarioCpfCnpj ?? string.Empty, row.UsuarioEmail ?? string.Empty, row.UsuarioTelefone ?? string.Empty, row.UsuarioUsuario ?? string.Empty, row.UsuarioSenha ?? string.Empty, row.UsuarioAtivo ?? false)) : null;
        var venda = row.VendaId.HasValue ? new Venda(row.VendaId.Value) : null;
        var movimentacao = new MovimentacoesEstoques(row.Id, row.DataMovimentacao, row.TipoMovimentacao, usuario, null, venda, row.Observacao, row.Status);

        var itensDbRow = (await _session.Connection.QueryAsync<MovimentacaoItemDbRow, Produtos, Categorias, Marcas, UnidadesMedida, MovimentacaoItemDbRow>(
            itensSql,
            (itemDbRow, produto, categoria, marca, unidadeMedida) =>
            {
                itemDbRow.Produto = new Produtos(produto.Id, produto.Produto, produto.Descricao, categoria, marca, unidadeMedida);
                return itemDbRow;
            },
            new { Id = id },
            splitOn: "id,id,id,id",
            transaction: _session.Transaction)).ToList();

        const string atributosSql = @"
            SELECT savr.sku AS Sku, sav.chave_id AS ChaveId, sav.valor AS Valor,
                   sav.id AS Id, sak.chave AS Chave
            FROM skus_atributos_valores_relacionamento savr
            JOIN sku_atributos_valores sav ON sav.id = savr.valor_id
            JOIN sku_atributos_chaves sak ON sak.id = sav.chave_id
            WHERE savr.sku IN (SELECT sku FROM movimentacoes_estoque_itens WHERE movimentacao_estoque_id = @Id);";

        var atributosDbRow = (await _session.Connection.QueryAsync<AtributoDbRow>(
            atributosSql, new { Id = id }, transaction: _session.Transaction)).ToList();

        var atributosPorSku = atributosDbRow.GroupBy(a => a.Sku).ToDictionary(g => g.Key, g => g.AsEnumerable());

        foreach (var itemDbRow in itensDbRow)
        {
            movimentacao.AdicionarItemExistente(BuildItem(itemDbRow, id, atributosPorSku.GetValueOrDefault(itemDbRow.SkuCodigo, Enumerable.Empty<AtributoDbRow>())));
        }

        return movimentacao;
    }

    public async Task<MovimentacoesEstoques> CriarMovimentacao(MovimentacoesEstoques movimentacao)
    {
        try
        {
            const string sql = @"
                INSERT INTO movimentacoes_estoque (data_movimentacao, tipo_movimentacao, status, observacao, usuario_id, nfe_id, venda_id)
                VALUES (@DataMovimentacao, @TipoMovimentacao::tipo_movimentacao_estoque_enum, @Status::status_movimentacao_estoque_enum, @Observacao, @UsuarioId, @NfeId, @VendaId)
                RETURNING id;";

            var idGerado = await _session.Connection.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    movimentacao.DataMovimentacao,
                    TipoMovimentacao = movimentacao.TipoMovimentacao.ToString(),
                    Status = movimentacao.Status.ToString(),
                    movimentacao.Observacao,
                    UsuarioId = movimentacao.Usuario?.Id,
                    NfeId = movimentacao.Nfe?.Id,
                    VendaId = movimentacao.Venda?.Id
                },
                transaction: _session.Transaction);

            await InserirItens(idGerado, movimentacao.MovimentacoesEstoquesItens);

            var persisted = new MovimentacoesEstoques(idGerado, movimentacao.DataMovimentacao, movimentacao.TipoMovimentacao, movimentacao.Usuario, movimentacao.Nfe, movimentacao.Venda, movimentacao.Observacao, movimentacao.Status);
            foreach (var item in movimentacao.MovimentacoesEstoquesItens)
            {
                persisted.AdicionarItemExistente(new MovimentacoesEstoquesItens(item.Id, idGerado, item.Sku, item.Quantidade, item.CustoUnitario, item.QuantidadeAnterior, item.CustoMedioAnterior, item.ProdutoNome, item.UnidadeMedidaSigla));
            }

            return persisted;
        }
        catch (PostgresException ex)
        {
            throw DbExceptionTranslator.Translate(ex);
        }
    }

    public async Task<MovimentacoesEstoques> AtualizarMovimentacao(int id, MovimentacoesEstoques movimentacao)
    {
        try
        {
            const string sql = @"
                UPDATE movimentacoes_estoque
                SET data_movimentacao = @DataMovimentacao,
                    tipo_movimentacao = @TipoMovimentacao::tipo_movimentacao_estoque_enum,
                    status = @Status::status_movimentacao_estoque_enum,
                    observacao = @Observacao,
                    usuario_id = @UsuarioId,
                    nfe_id = @NfeId,
                    venda_id = @VendaId
                WHERE id = @Id;";

            await _session.Connection.ExecuteAsync(
                sql,
                new
                {
                    Id = id,
                    movimentacao.DataMovimentacao,
                    TipoMovimentacao = movimentacao.TipoMovimentacao.ToString(),
                    Status = movimentacao.Status.ToString(),
                    movimentacao.Observacao,
                    UsuarioId = movimentacao.Usuario?.Id,
                    NfeId = movimentacao.Nfe?.Id,
                    VendaId = movimentacao.Venda?.Id
                },
                transaction: _session.Transaction);

            var updated = new MovimentacoesEstoques(id, movimentacao.DataMovimentacao, movimentacao.TipoMovimentacao, movimentacao.Usuario, movimentacao.Nfe, movimentacao.Venda, movimentacao.Observacao, movimentacao.Status);

            await ReplacerItens(id, movimentacao.MovimentacoesEstoquesItens);
            foreach (var item in movimentacao.MovimentacoesEstoquesItens)
            {
                updated.AdicionarItemExistente(new MovimentacoesEstoquesItens(item.Id, id, item.Sku, item.Quantidade, item.CustoUnitario, item.QuantidadeAnterior, item.CustoMedioAnterior, item.ProdutoNome, item.UnidadeMedidaSigla));
            }

            return updated;
        }
        catch (PostgresException ex)
        {
            throw DbExceptionTranslator.Translate(ex);
        }
    }

    public async Task<bool> DeletarMovimentacao(int id)
    {
        try
        {
            await _session.Connection.ExecuteAsync(
                "DELETE FROM movimentacoes_estoque_itens WHERE movimentacao_estoque_id = @Id;",
                new { Id = id }, transaction: _session.Transaction);

            var linhasAfetadas = await _session.Connection.ExecuteAsync(
                "DELETE FROM movimentacoes_estoque WHERE id = @Id;",
                new { Id = id }, transaction: _session.Transaction);

            return linhasAfetadas > 0;
        }
        catch (PostgresException ex)
        {
            throw DbExceptionTranslator.Translate(ex);
        }
    }

    public async Task<ResultadoPaginado<MovimentacoesEstoques>> PesquisarMovimentacoes(string termo, int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string countSql = @"
            SELECT COUNT(*)
            FROM movimentacoes_estoque
            WHERE observacao ILIKE @Termo OR tipo_movimentacao::text ILIKE @Termo OR status::text ILIKE @Termo;";

        const string movimentacoesSql = @"
            SELECT me.id, me.data_movimentacao, me.tipo_movimentacao, me.status, me.observacao,
                   u.id AS UsuarioId, u.nome AS UsuarioNome, u.cpf_cnpj AS UsuarioCpfCnpj, u.email AS UsuarioEmail,
                   u.telefone AS UsuarioTelefone, u.usuario AS UsuarioUsuario, u.senha AS UsuarioSenha, u.ativo AS UsuarioAtivo,
                   me.venda_id AS VendaId
            FROM movimentacoes_estoque me
            LEFT JOIN usuarios u ON u.id = me.usuario_id
            WHERE me.observacao ILIKE @Termo OR me.tipo_movimentacao::text ILIKE @Termo OR me.status::text ILIKE @Termo
            ORDER BY me.data_movimentacao DESC
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        var total = await _session.Connection.ExecuteScalarAsync<int>(countSql, new { Termo = $"%{termo}%" }, transaction: _session.Transaction);

        var movimentacoesDbRow = (await _session.Connection.QueryAsync<MovimentacaoDbRow>(
            movimentacoesSql,
            new { Termo = $"%{termo}%", TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction)).ToList();

        if (!movimentacoesDbRow.Any())
        {
            return new ResultadoPaginado<MovimentacoesEstoques>(Enumerable.Empty<MovimentacoesEstoques>(), total, pagina, tamanhoDaPagina);
        }

        var ids = movimentacoesDbRow.Select(m => m.Id).ToArray();

        const string itensSql = @"
            SELECT mei.id, mei.quantidade, mei.custo_unitario, mei.movimentacao_estoque_id AS MovimentacaoId,
                   s.sku AS SkuCodigo, s.gtin_ean AS SkuGtinEan, s.preco AS SkuPreco, s.estoque AS SkuEstoque, s.ativo AS SkuAtivo,
                   s.custo_medio AS SkuCustoMedio, s.custo_ultima_compra AS SkuCustoUltimaCompra,
                   mei.quantidade_anterior AS QuantidadeAnterior, mei.custo_medio_anterior AS CustoMedioAnterior,
                   p.id, p.produto, p.descricao, p.ativo,
                   c.id, c.categoria, c.descricao, c.ativo,
                   m.id, m.marca, m.descricao, m.ativo,
                   u.id, u.sigla, u.descricao, u.categoria, u.permite_decimais AS PermiteDecimais, u.ativo
            FROM movimentacoes_estoque_itens mei
            JOIN skus s ON s.sku = mei.sku
            JOIN produtos p ON p.id = s.produto_id
            JOIN categorias c ON c.id = p.categoria_id
            JOIN marcas m ON m.id = p.marca_id
            JOIN unidades_medida u ON u.id = p.unidade_medida_id
            WHERE mei.movimentacao_estoque_id = ANY(@Ids);";

        var itensDbRow = (await _session.Connection.QueryAsync<MovimentacaoItemDbRow, Produtos, Categorias, Marcas, UnidadesMedida, MovimentacaoItemDbRow>(
            itensSql,
            (itemDbRow, produto, categoria, marca, unidadeMedida) =>
            {
                itemDbRow.Produto = new Produtos(produto.Id, produto.Produto, produto.Descricao, categoria, marca, unidadeMedida);
                return itemDbRow;
            },
            new { Ids = ids },
            splitOn: "id,id,id,id",
            transaction: _session.Transaction)).ToList();

        const string atributosSql = @"
            SELECT savr.sku AS Sku, sav.chave_id AS ChaveId, sav.valor AS Valor,
                   sav.id AS Id, sak.chave AS Chave
            FROM skus_atributos_valores_relacionamento savr
            JOIN sku_atributos_valores sav ON sav.id = savr.valor_id
            JOIN sku_atributos_chaves sak ON sak.id = sav.chave_id
            WHERE savr.sku IN (SELECT sku FROM movimentacoes_estoque_itens WHERE movimentacao_estoque_id = ANY(@Ids));";

        var atributosDbRow = (await _session.Connection.QueryAsync<AtributoDbRow>(
            atributosSql, new { Ids = ids }, transaction: _session.Transaction)).ToList();
        
        var atributosPorSku = atributosDbRow.GroupBy(a => a.Sku).ToDictionary(g => g.Key, g => g.AsEnumerable());

        var itensPorMovimentacao = itensDbRow
            .GroupBy(i => i.MovimentacaoId)
            .ToDictionary(g => g.Key, g => g.AsEnumerable());

        var movimentacoes = movimentacoesDbRow.Select(row =>
        {
            var usuario = row.UsuarioId.HasValue ? BuildUsuario(new UsuarioDbRow(row.UsuarioId.Value, row.UsuarioNome ?? string.Empty, row.UsuarioCpfCnpj ?? string.Empty, row.UsuarioEmail ?? string.Empty, row.UsuarioTelefone ?? string.Empty, row.UsuarioUsuario ?? string.Empty, row.UsuarioSenha ?? string.Empty, row.UsuarioAtivo ?? false)) : null;
            var venda = row.VendaId.HasValue ? new Venda(row.VendaId.Value) : null;
            var movimentacao = new MovimentacoesEstoques(row.Id, row.DataMovimentacao, row.TipoMovimentacao, usuario, null, venda, row.Observacao, row.Status);

            if (itensPorMovimentacao.TryGetValue(row.Id, out var itens))
            {
                foreach (var itemDbRow in itens)
                {
                    movimentacao.AdicionarItemExistente(BuildItem(itemDbRow, row.Id, atributosPorSku.GetValueOrDefault(itemDbRow.SkuCodigo, Enumerable.Empty<AtributoDbRow>())));
                }
            }

            return movimentacao;
        }).ToList();

        return new ResultadoPaginado<MovimentacoesEstoques>(movimentacoes, total, pagina, tamanhoDaPagina);
    }

    private async Task InserirItens(int movimentacaoId, IEnumerable<MovimentacoesEstoquesItens> itens)
    {
        const string sql = @"
            INSERT INTO movimentacoes_estoque_itens (quantidade, custo_unitario, sku, movimentacao_estoque_id, quantidade_anterior, custo_medio_anterior)
            VALUES (@Quantidade, @CustoUnitario, @SkuCodigo, @MovimentacaoId, @QuantidadeAnterior, @CustoMedioAnterior);";

        await _session.Connection.ExecuteAsync(
            sql,
            itens.Select(i => new
            {
                i.Quantidade,
                i.CustoUnitario,
                SkuCodigo = i.Sku.Sku,
                MovimentacaoId = movimentacaoId,
                i.QuantidadeAnterior,
                i.CustoMedioAnterior
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

    private static MovimentacoesEstoquesItens BuildItem(MovimentacaoItemDbRow row, int movimentacaoId, IEnumerable<AtributoDbRow> atributosDbRow)
    {
        var sku = new Skus(row.SkuCodigo, row.SkuPreco, row.SkuEstoque, row.SkuAtivo, row.SkuGtinEan, row.SkuCustoMedio, row.SkuCustoUltimaCompra, row.Produto);
        
        foreach (var attr in atributosDbRow)
        {
            sku.AdicionarAtributo(new SkuAtributosValores(attr.Id, attr.ChaveId, attr.Valor));
        }

        if (!row.SkuAtivo)
            sku.Desativar();

        return new MovimentacoesEstoquesItens(row.Id, movimentacaoId, sku, row.Quantidade, row.CustoUnitario, row.QuantidadeAnterior, row.CustoMedioAnterior, sku.NomeExibicao, sku.Produto!.UnidadeMedida.Sigla);
    }

    private static Usuarios BuildUsuario(UsuarioDbRow row)
    {
        return new Usuarios(row.Id, row.Nome, row.CpfCnpj, row.Email, row.Usuario, row.Senha, row.Telefone, row.Ativo);
    }

    private sealed record MovimentacaoDbRow(int Id, DateTime DataMovimentacao, TipoMovimentacaoEstoque TipoMovimentacao, StatusMovimentacaoEstoque Status, string? Observacao,
        int? UsuarioId, string? UsuarioNome, string? UsuarioCpfCnpj, string? UsuarioEmail, string? UsuarioTelefone,
        string? UsuarioUsuario, string? UsuarioSenha, bool? UsuarioAtivo, int? VendaId);

    private sealed class MovimentacaoItemDbRow
    {
        public int Id { get; set; }
        public decimal Quantidade { get; set; }
        public decimal CustoUnitario { get; set; }
        public int MovimentacaoId { get; set; }
        public string SkuCodigo { get; set; } = null!;
        public string? SkuGtinEan { get; set; }
        public decimal SkuPreco { get; set; }
        public decimal SkuEstoque { get; set; }
        public bool SkuAtivo { get; set; }
        public decimal SkuCustoMedio { get; set; }
        public decimal SkuCustoUltimaCompra { get; set; }
        public decimal? QuantidadeAnterior { get; set; }
        public decimal? CustoMedioAnterior { get; set; }
        public string ProdutoNome { get; set; } = null!;
        public string UnidadeMedidaSigla { get; set; } = null!;
        public Produtos? Produto { get; set; }
    }

    private sealed record UsuarioDbRow(int Id, string Nome, string CpfCnpj, string Email, string Telefone,
        string Usuario, string Senha, bool Ativo);
    private sealed record AtributoDbRow(string Sku, int ChaveId, string Valor, int Id, string Chave);
}
