using Backend.Core.Common.Results;
using Backend.Core.Features.Catalogo.Entities;
using Backend.Core.Features.Compras.Entities;
using Backend.Core.Features.Compras.Repositories;
using Backend.Core.Features.Parceiros.Entities;
using Backend.Infrastructure.PostgreSQL.Common;
using Dapper;

namespace Backend.Infrastructure.PostgreSQL.Features.Compras;

public class ComprasRepository : IComprasRepository
{
    private readonly DbSession _session;

    public ComprasRepository(DbSession session)
    {
        _session = session;
    }

    private const string CompraColunas = @"
        c.id AS Id, c.data_compra, c.valor_nota, c.valor_frete, c.valor_seguro, c.valor_outras_despesas,
        c.valor_desconto, c.valor_frete_terceiro, c.transportadora_id, c.condicao_pagamento_id, c.valor_total, c.observacao,
        c.estornada_em, c.motivo_estorno, c.usuario_estorno_id,
        (SELECT n.id FROM nfes n WHERE n.compra_id = c.id) AS NfeId,
        e.id AS Id, e.nome_razaosocial, e.cpf_cnpj, e.ativo,
        f.id AS Id, f.nome_razaosocial, f.cpf_cnpj, f.ativo";

    private const string CompraFrom = @"
        FROM compras c
        JOIN emitentes e ON e.id = c.emitente_id
        JOIN fornecedores f ON f.id = c.fornecedor_id";

    public async Task<ResultadoPaginado<Compra>> PesquisarCompras(string? termo, int pagina = 1, int tamanhoDaPagina = 20, int? fornecedorId = null)
    {
        var p = new { Termo = string.IsNullOrWhiteSpace(termo) ? null : $"%{termo}%", FornecedorId = fornecedorId, TamanhoDaPagina = tamanhoDaPagina, Offset = (pagina - 1) * tamanhoDaPagina };
        const string where = @"
            WHERE (@Termo::text IS NULL OR f.nome_razaosocial ILIKE @Termo OR e.nome_razaosocial ILIKE @Termo OR c.observacao ILIKE @Termo)
              AND (@FornecedorId::int IS NULL OR c.fornecedor_id = @FornecedorId::int)";

        var total = await _session.Connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) " + CompraFrom + where, p, transaction: _session.Transaction);

        var compras = (await _session.Connection.QueryAsync<Compra, Emitentes, Fornecedores, Compra>(
            "SELECT " + CompraColunas + CompraFrom + where + " ORDER BY c.data_compra DESC, c.id DESC LIMIT @TamanhoDaPagina OFFSET @Offset;",
            Mapear, p, transaction: _session.Transaction, splitOn: "Id,Id")).ToList();

        if (compras.Count > 0)
        {
            var itens = await ObterItens(compras.Select(c => c.Id).ToArray());
            foreach (var c in compras)
                c.DefinirItens(itens.Where(i => i.CompraId == c.Id).ToList());
        }

        return new ResultadoPaginado<Compra>(compras, total, pagina, tamanhoDaPagina);
    }

    public async Task<Compra?> ObterCompraPorId(int id)
    {
        var compra = (await _session.Connection.QueryAsync<Compra, Emitentes, Fornecedores, Compra>(
            "SELECT " + CompraColunas + CompraFrom + " WHERE c.id = @Id;",
            Mapear, new { Id = id }, transaction: _session.Transaction, splitOn: "Id,Id")).SingleOrDefault();

        if (compra is null) return null;

        compra.DefinirItens(await ObterItens(new[] { id }));
        return compra;
    }

    public async Task<Compra> CriarCompra(Compra compra)
    {
        const string sql = @"
            INSERT INTO compras (emitente_id, fornecedor_id, data_compra, valor_nota, valor_frete, valor_seguro,
                                 valor_outras_despesas, valor_desconto, valor_frete_terceiro, transportadora_id,
                                 valor_total, observacao, condicao_pagamento_id)
            VALUES (@EmitenteId, @FornecedorId, @DataCompra, @ValorNota, @ValorFrete, @ValorSeguro,
                    @ValorOutrasDespesas, @ValorDesconto, @ValorFreteTerceiro, @TransportadoraId,
                    @ValorTotal, @Observacao, @CondicaoPagamentoId)
            RETURNING id;";

        compra.Id = await _session.Connection.ExecuteScalarAsync<int>(
            sql,
            new
            {
                EmitenteId = compra.Emitente.Id,
                FornecedorId = compra.Fornecedor.Id,
                compra.DataCompra, compra.ValorNota, compra.ValorFrete, compra.ValorSeguro,
                compra.ValorOutrasDespesas, compra.ValorDesconto, compra.ValorFreteTerceiro, compra.TransportadoraId,
                compra.ValorTotal, compra.Observacao, compra.CondicaoPagamentoId
            },
            transaction: _session.Transaction);

        const string itemSql = @"
            INSERT INTO compras_itens (compra_id, sku, quantidade, valor_unitario, valor_desconto, valor_total, valor_rateio, custo_unitario)
            VALUES (@CompraId, @SkuCodigo, @Quantidade, @ValorUnitario, @ValorDesconto, @ValorTotal, @ValorRateio, @CustoUnitario);";

        await _session.Connection.ExecuteAsync(
            itemSql,
            compra.Itens.Select(i => new
            {
                CompraId = compra.Id, SkuCodigo = i.Sku.Sku,
                i.Quantidade, i.ValorUnitario, i.ValorDesconto, i.ValorTotal, i.ValorRateio, i.CustoUnitario
            }),
            transaction: _session.Transaction);

        return compra;
    }

    public async Task<bool> MarcarEstornada(Compra compra)
        => await _session.Connection.ExecuteAsync(
            @"UPDATE compras SET estornada_em = @EstornadaEm, motivo_estorno = @MotivoEstorno, usuario_estorno_id = @UsuarioEstornoId
              WHERE id = @Id AND estornada_em IS NULL;",
            new { compra.Id, compra.EstornadaEm, compra.MotivoEstorno, compra.UsuarioEstornoId }, transaction: _session.Transaction) > 0;

    public async Task<IReadOnlyDictionary<string, string>> ObterVinculosDoFornecedor(int fornecedorId)
    {
        var rows = await _session.Connection.QueryAsync<(string Codigo, string Sku)>(
            "SELECT codigo_produto AS Codigo, sku AS Sku FROM fornecedores_skus WHERE fornecedor_id = @FornecedorId;",
            new { FornecedorId = fornecedorId }, transaction: _session.Transaction);
        return rows.ToDictionary(r => r.Codigo, r => r.Sku);
    }

    public Task VincularSku(int fornecedorId, string codigoProduto, string sku)
        => _session.Connection.ExecuteAsync(@"
            INSERT INTO fornecedores_skus (fornecedor_id, codigo_produto, sku)
            VALUES (@FornecedorId, @CodigoProduto, @Sku)
            ON CONFLICT (fornecedor_id, codigo_produto) DO UPDATE SET sku = EXCLUDED.sku;",
            new { FornecedorId = fornecedorId, CodigoProduto = codigoProduto, Sku = sku }, transaction: _session.Transaction);

    private static Compra Mapear(Compra compra, Emitentes emitente, Fornecedores fornecedor)
    {
        compra.AtualizarEmitente(emitente);
        compra.AtualizarFornecedor(fornecedor);
        compra.DefinirItens(new List<CompraItens>());
        return compra;
    }

    private async Task<List<CompraItens>> ObterItens(int[] compraIds)
    {
        const string sql = @"
            SELECT ci.id, ci.compra_id, ci.quantidade, ci.valor_unitario, ci.valor_desconto, ci.valor_total,
                   ci.valor_rateio, ci.custo_unitario,
                   s.sku, s.gtin_ean, s.preco, s.estoque, s.ativo,
                   p.id, p.produto,
                   u.id, u.sigla, u.permite_decimais
            FROM compras_itens ci
            JOIN skus s ON s.sku = ci.sku
            JOIN produtos p ON p.id = s.produto_id
            JOIN unidades_medida u ON u.id = p.unidade_medida_id
            WHERE ci.compra_id = ANY(@Ids)
            ORDER BY ci.compra_id, ci.id;";

        return (await _session.Connection.QueryAsync<CompraItens, Skus, Produtos, UnidadesMedida, CompraItens>(
            sql,
            (item, sku, produto, unidade) =>
            {
                produto.DefinirUnidadeMedida(unidade);
                sku.AtualizarProduto(produto);
                item.AtualizarSku(sku);
                return item;
            },
            new { Ids = compraIds }, transaction: _session.Transaction, splitOn: "sku,id,id")).ToList();
    }
}
