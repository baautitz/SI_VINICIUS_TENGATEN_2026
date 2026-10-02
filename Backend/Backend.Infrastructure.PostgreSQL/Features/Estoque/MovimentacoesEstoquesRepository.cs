using System.Linq;
using Backend.Core.Common.Results;
using Backend.Core.Features.Acesso.Entities;
using Backend.Core.Features.Catalogo.Entities;
using Backend.Core.Features.Estoque.Entities;
using Backend.Core.Features.Estoque.Entities.Enums;
using Backend.Core.Features.Estoque.Repositories;
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

    private const string SelecaoMovimentacao = @"
        SELECT me.id, me.data_movimentacao, me.tipo_movimentacao, me.origem_tipo, me.origem_id, me.motivo, me.observacao,
               EXISTS (SELECT 1 FROM movimentacoes_estoque e WHERE e.origem_tipo = 'ESTORNO' AND e.origem_id = me.id) AS Estornada,
               u.id AS UsuarioId, u.nome AS UsuarioNome, u.cpf_cnpj AS UsuarioCpfCnpj, u.email AS UsuarioEmail,
               u.telefone AS UsuarioTelefone, u.usuario AS UsuarioUsuario, u.senha AS UsuarioSenha, u.ativo AS UsuarioAtivo
        FROM movimentacoes_estoque me
        LEFT JOIN usuarios u ON u.id = me.usuario_id";

    public Task<ResultadoPaginado<MovimentacoesEstoques>> ObterMovimentacoes(int pagina = 1, int tamanhoDaPagina = 20)
        => Listar("TRUE", new { }, pagina, tamanhoDaPagina);

    public Task<ResultadoPaginado<MovimentacoesEstoques>> PesquisarMovimentacoes(string termo, int pagina = 1, int tamanhoDaPagina = 20)
        => Listar(@"(me.observacao ILIKE @Termo OR me.motivo ILIKE @Termo
                     OR me.tipo_movimentacao::text ILIKE @Termo OR me.origem_tipo::text ILIKE @Termo
                     OR me.id::text = @Exato
                     OR EXISTS (SELECT 1 FROM movimentacoes_estoque_itens i WHERE i.movimentacao_estoque_id = me.id AND i.sku ILIKE @Termo))",
            new { Termo = $"%{termo}%", Exato = termo.Trim() }, pagina, tamanhoDaPagina);

    public async Task<MovimentacoesEstoques?> ObterMovimentacaoPorId(int id)
        => await ObterUma("me.id = @Id", new { Id = id });

    public async Task<IReadOnlyList<MovimentacoesEstoques>> ObterMovimentacoesPorOrigem(OrigemMovimentacaoEstoque origemTipo, int origemId)
    {
        var rows = (await _session.Connection.QueryAsync<MovimentacaoDbRow>(
            $"{SelecaoMovimentacao} WHERE me.origem_tipo = @Origem::origem_movimentacao_estoque_enum AND me.origem_id = @OrigemId ORDER BY me.id;",
            new { Origem = origemTipo.ToString(), OrigemId = origemId }, transaction: _session.Transaction)).ToList();
        return await Montar(rows);
    }

    public async Task<MovimentacoesEstoques> CriarMovimentacao(MovimentacoesEstoques movimentacao)
    {
        try
        {
            const string sql = @"
                INSERT INTO movimentacoes_estoque (data_movimentacao, tipo_movimentacao, origem_tipo, origem_id, motivo, observacao, usuario_id)
                VALUES (@DataMovimentacao, @TipoMovimentacao::tipo_movimentacao_estoque_enum, @OrigemTipo::origem_movimentacao_estoque_enum, @OrigemId, @Motivo, @Observacao, @UsuarioId)
                RETURNING id;";

            var idGerado = await _session.Connection.ExecuteScalarAsync<int>(
                sql,
                new
                {
                    movimentacao.DataMovimentacao,
                    TipoMovimentacao = movimentacao.TipoMovimentacao.ToString(),
                    OrigemTipo = movimentacao.OrigemTipo.ToString(),
                    movimentacao.OrigemId,
                    movimentacao.Motivo,
                    movimentacao.Observacao,
                    UsuarioId = movimentacao.Usuario?.Id
                },
                transaction: _session.Transaction);

            await InserirItens(idGerado, movimentacao.MovimentacoesEstoquesItens);

            var persistida = new MovimentacoesEstoques(idGerado, movimentacao.DataMovimentacao, movimentacao.TipoMovimentacao, movimentacao.OrigemTipo, movimentacao.OrigemId, movimentacao.Motivo, movimentacao.Observacao, movimentacao.Usuario, false);
            foreach (var item in movimentacao.MovimentacoesEstoquesItens)
            {
                persistida.AdicionarItemExistente(new MovimentacoesEstoquesItens(item.Id, idGerado, item.Sku, item.Quantidade, item.CustoUnitario, item.QuantidadeAnterior, item.CustoMedioAnterior, item.ProdutoNome, item.UnidadeMedidaSigla));
            }

            return persistida;
        }
        catch (PostgresException ex)
        {
            throw DbExceptionTranslator.Translate(ex);
        }
    }

    private async Task<MovimentacoesEstoques?> ObterUma(string where, object param)
    {
        var rows = (await _session.Connection.QueryAsync<MovimentacaoDbRow>(
            $"{SelecaoMovimentacao} WHERE {where};", param, transaction: _session.Transaction)).ToList();
        return (await Montar(rows)).FirstOrDefault();
    }

    private async Task<ResultadoPaginado<MovimentacoesEstoques>> Listar(string where, object param, int pagina, int tamanhoDaPagina)
    {
        var parametros = new DynamicParameters(param);
        parametros.Add("TamanhoDaPagina", tamanhoDaPagina);
        parametros.Add("Offset", (pagina - 1) * tamanhoDaPagina);

        var total = await _session.Connection.ExecuteScalarAsync<int>(
            $"SELECT COUNT(*) FROM movimentacoes_estoque me WHERE {where};", parametros, transaction: _session.Transaction);

        var rows = (await _session.Connection.QueryAsync<MovimentacaoDbRow>(
            $"{SelecaoMovimentacao} WHERE {where} ORDER BY me.data_movimentacao DESC, me.id DESC LIMIT @TamanhoDaPagina OFFSET @Offset;",
            parametros, transaction: _session.Transaction)).ToList();

        return new ResultadoPaginado<MovimentacoesEstoques>(await Montar(rows), total, pagina, tamanhoDaPagina);
    }

    private async Task<List<MovimentacoesEstoques>> Montar(List<MovimentacaoDbRow> rows)
    {
        if (rows.Count == 0) return new List<MovimentacoesEstoques>();

        var ids = rows.Select(r => r.Id).ToArray();

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
            WHERE mei.movimentacao_estoque_id = ANY(@Ids)
            ORDER BY mei.id ASC;";

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

        var atributosPorSku = (await _session.Connection.QueryAsync<AtributoDbRow>(
                atributosSql, new { Ids = ids }, transaction: _session.Transaction))
            .GroupBy(a => a.Sku).ToDictionary(g => g.Key, g => g.AsEnumerable());

        var itensPorMovimentacao = itensDbRow.GroupBy(i => i.MovimentacaoId).ToDictionary(g => g.Key, g => g.AsEnumerable());

        return rows.Select(row =>
        {
            var usuario = row.UsuarioId.HasValue
                ? new Usuarios(row.UsuarioId.Value, row.UsuarioNome ?? string.Empty, row.UsuarioCpfCnpj ?? string.Empty, row.UsuarioEmail ?? string.Empty, row.UsuarioUsuario ?? string.Empty, row.UsuarioSenha ?? string.Empty, row.UsuarioTelefone ?? string.Empty, row.UsuarioAtivo ?? false)
                : null;
            var movimentacao = new MovimentacoesEstoques(row.Id, row.DataMovimentacao, row.TipoMovimentacao, row.OrigemTipo, row.OrigemId, row.Motivo, row.Observacao, usuario, row.Estornada);

            if (itensPorMovimentacao.TryGetValue(row.Id, out var itens))
            {
                foreach (var itemDbRow in itens)
                    movimentacao.AdicionarItemExistente(BuildItem(itemDbRow, row.Id, atributosPorSku.GetValueOrDefault(itemDbRow.SkuCodigo, Enumerable.Empty<AtributoDbRow>())));
            }

            return movimentacao;
        }).ToList();
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

    private sealed record MovimentacaoDbRow(int Id, DateTime DataMovimentacao, TipoMovimentacaoEstoque TipoMovimentacao, OrigemMovimentacaoEstoque OrigemTipo, int? OrigemId, string? Motivo, string? Observacao, bool Estornada,
        int? UsuarioId, string? UsuarioNome, string? UsuarioCpfCnpj, string? UsuarioEmail, string? UsuarioTelefone,
        string? UsuarioUsuario, string? UsuarioSenha, bool? UsuarioAtivo);

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

    private sealed record AtributoDbRow(string Sku, int ChaveId, string Valor, int Id, string Chave);
}
