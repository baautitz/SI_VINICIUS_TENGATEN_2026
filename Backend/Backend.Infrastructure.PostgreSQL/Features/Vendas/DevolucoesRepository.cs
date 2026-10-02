using Backend.Core.Features.Vendas.Entities;
using Backend.Core.Features.Vendas.Repositories;
using Backend.Infrastructure.PostgreSQL.Common;
using Dapper;

namespace Backend.Infrastructure.PostgreSQL.Features.Vendas;

public class DevolucoesRepository : IDevolucoesRepository
{
    private readonly DbSession _session;

    public DevolucoesRepository(DbSession session)
    {
        _session = session;
    }

    private const string SelecaoDevolucao = "SELECT id, venda_id AS VendaId, data_devolucao AS DataDevolucao, motivo AS Motivo, valor_total AS ValorTotal FROM devolucoes";

    public async Task<Devolucao?> ObterDevolucaoPorId(int id)
        => (await Montar(await Consultar("WHERE id = @Id", new { Id = id }))).SingleOrDefault();

    public async Task<IReadOnlyList<Devolucao>> ObterDevolucoesPorVenda(int vendaId)
        => await Montar(await Consultar("WHERE venda_id = @VendaId ORDER BY id DESC", new { VendaId = vendaId }));

    public async Task<Devolucao> CriarDevolucao(Devolucao devolucao)
    {
        var id = await _session.Connection.ExecuteScalarAsync<int>(
            @"INSERT INTO devolucoes (venda_id, data_devolucao, motivo, valor_total)
              VALUES (@VendaId, @DataDevolucao, @Motivo, @ValorTotal) RETURNING id;",
            devolucao, transaction: _session.Transaction);

        await _session.Connection.ExecuteAsync(
            @"INSERT INTO devolucoes_itens (devolucao_id, venda_item_id, sku, quantidade, valor_unitario, custo_unitario)
              VALUES (@DevolucaoId, @VendaItemId, @Sku, @Quantidade, @ValorUnitario, @CustoUnitario);",
            devolucao.Itens.Select(i => new { DevolucaoId = id, i.VendaItemId, i.Sku, i.Quantidade, i.ValorUnitario, i.CustoUnitario }),
            transaction: _session.Transaction);

        devolucao.Id = id;
        return devolucao;
    }

    private async Task<List<DevolucaoRow>> Consultar(string filtro, object param)
        => (await _session.Connection.QueryAsync<DevolucaoRow>($"{SelecaoDevolucao} {filtro};", param, transaction: _session.Transaction)).ToList();

    private async Task<IReadOnlyList<Devolucao>> Montar(List<DevolucaoRow> rows)
    {
        if (rows.Count == 0) return new List<Devolucao>();

        var itens = (await _session.Connection.QueryAsync<ItemRow>(
            @"SELECT id, devolucao_id AS DevolucaoId, venda_item_id AS VendaItemId, sku AS Sku, quantidade AS Quantidade,
                     valor_unitario AS ValorUnitario, custo_unitario AS CustoUnitario
              FROM devolucoes_itens WHERE devolucao_id = ANY(@Ids) ORDER BY id;",
            new { Ids = rows.Select(r => r.Id).ToArray() }, transaction: _session.Transaction)).ToLookup(i => i.DevolucaoId);

        return rows.Select(r =>
        {
            var d = new Devolucao(r.Id, r.VendaId, r.DataDevolucao, r.Motivo, r.ValorTotal);
            foreach (var i in itens[r.Id])
                d.AdicionarItemExistente(new DevolucaoItens(i.Id, i.VendaItemId, i.Sku, i.Quantidade, i.ValorUnitario, i.CustoUnitario));
            return d;
        }).ToList();
    }

    private sealed record DevolucaoRow(int Id, int VendaId, DateTime DataDevolucao, string Motivo, decimal ValorTotal);

    private sealed record ItemRow(int Id, int DevolucaoId, int VendaItemId, string Sku, decimal Quantidade, decimal ValorUnitario, decimal CustoUnitario);
}
