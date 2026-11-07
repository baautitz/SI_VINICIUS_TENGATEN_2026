using Backend.Core.Features.Compras.Entities;
using Backend.Core.Features.Compras.Repositories;
using Backend.Infrastructure.PostgreSQL.Common;
using Dapper;

namespace Backend.Infrastructure.PostgreSQL.Features.Compras;

public class DevolucoesCompraRepository : IDevolucoesCompraRepository
{
    private readonly DbSession _session;

    public DevolucoesCompraRepository(DbSession session)
    {
        _session = session;
    }

    private const string SelecaoDevolucao = "SELECT id, compra_id AS CompraId, data_devolucao AS DataDevolucao, motivo AS Motivo, valor_total AS ValorTotal FROM devolucoes_compra";

    public async Task<DevolucaoCompra?> ObterDevolucaoPorId(int id)
        => (await Montar(await Consultar("WHERE id = @Id", new { Id = id }))).SingleOrDefault();

    public async Task<IReadOnlyList<DevolucaoCompra>> ObterDevolucoesPorCompra(int compraId)
        => await Montar(await Consultar("WHERE compra_id = @CompraId ORDER BY id DESC", new { CompraId = compraId }));

    public async Task<DevolucaoCompra> CriarDevolucao(DevolucaoCompra devolucao)
    {
        var id = await _session.Connection.ExecuteScalarAsync<int>(
            @"INSERT INTO devolucoes_compra (compra_id, data_devolucao, motivo, valor_total)
              VALUES (@CompraId, @DataDevolucao, @Motivo, @ValorTotal) RETURNING id;",
            devolucao, transaction: _session.Transaction);

        await _session.Connection.ExecuteAsync(
            @"INSERT INTO devolucoes_compra_itens (devolucao_compra_id, compra_item_id, sku, quantidade, valor_unitario, custo_unitario)
              VALUES (@DevolucaoId, @CompraItemId, @Sku, @Quantidade, @ValorUnitario, @CustoUnitario);",
            devolucao.Itens.Select(i => new { DevolucaoId = id, i.CompraItemId, i.Sku, i.Quantidade, i.ValorUnitario, i.CustoUnitario }),
            transaction: _session.Transaction);

        devolucao.Id = id;
        return devolucao;
    }

    public async Task CriarAbatimento(DevolucaoCompraAbatimento a)
        => await _session.Connection.ExecuteAsync(
            "INSERT INTO devolucao_compra_abatimentos (devolucao_compra_id, conta_pagar_id, valor) VALUES (@DevolucaoCompraId, @ContaPagarId, @Valor);",
            a, transaction: _session.Transaction);

    private async Task<List<DevolucaoRow>> Consultar(string filtro, object param)
        => (await _session.Connection.QueryAsync<DevolucaoRow>($"{SelecaoDevolucao} {filtro};", param, transaction: _session.Transaction)).ToList();

    private async Task<IReadOnlyList<DevolucaoCompra>> Montar(List<DevolucaoRow> rows)
    {
        if (rows.Count == 0) return new List<DevolucaoCompra>();

        var itens = (await _session.Connection.QueryAsync<ItemRow>(
            @"SELECT id, devolucao_compra_id AS DevolucaoId, compra_item_id AS CompraItemId, sku AS Sku, quantidade AS Quantidade,
                     valor_unitario AS ValorUnitario, custo_unitario AS CustoUnitario
              FROM devolucoes_compra_itens WHERE devolucao_compra_id = ANY(@Ids) ORDER BY id;",
            new { Ids = rows.Select(r => r.Id).ToArray() }, transaction: _session.Transaction)).ToLookup(i => i.DevolucaoId);

        return rows.Select(r =>
        {
            var d = new DevolucaoCompra(r.Id, r.CompraId, r.DataDevolucao, r.Motivo, r.ValorTotal);
            foreach (var i in itens[r.Id])
                d.AdicionarItemExistente(new DevolucaoCompraItens(i.Id, i.CompraItemId, i.Sku, i.Quantidade, i.ValorUnitario, i.CustoUnitario));
            return d;
        }).ToList();
    }

    private sealed record DevolucaoRow(int Id, int CompraId, DateTime DataDevolucao, string Motivo, decimal ValorTotal);

    private sealed record ItemRow(int Id, int DevolucaoId, int CompraItemId, string Sku, decimal Quantidade, decimal ValorUnitario, decimal CustoUnitario);
}
