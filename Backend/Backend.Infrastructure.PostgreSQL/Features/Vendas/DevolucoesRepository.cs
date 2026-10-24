using Backend.Core.Common.Results;
using Backend.Core.Features.Vendas.DTOs;
using Backend.Core.Features.Vendas.Entities;
using Backend.Core.Features.Vendas.Repositories;
using Backend.Infrastructure.PostgreSQL.Common;
using Backend.Core.Features.Vendas.Entities.Enums;
using Dapper;

namespace Backend.Infrastructure.PostgreSQL.Features.Vendas;

public class DevolucoesRepository : IDevolucoesRepository
{
    private readonly DbSession _session;

    public DevolucoesRepository(DbSession session)
    {
        _session = session;
    }

    private const string SelecaoDevolucao = "SELECT id, venda_id AS VendaId, data_devolucao AS DataDevolucao, motivo AS Motivo, valor_total AS ValorTotal, status AS Status, estornado_em AS EstornadoEm, motivo_estorno AS MotivoEstorno FROM devolucoes";

    public async Task<Devolucao?> ObterDevolucaoPorId(int id)
        => (await Montar(await Consultar("WHERE id = @Id", new { Id = id }))).SingleOrDefault();

    public async Task<IReadOnlyList<Devolucao>> ObterDevolucoesPorVenda(int vendaId)
        => await Montar(await Consultar("WHERE venda_id = @VendaId ORDER BY id DESC", new { VendaId = vendaId }));

    public async Task<ResultadoPaginado<DevolucaoResumo>> PesquisarDevolucoes(string termo, int pagina = 1, int tamanhoDaPagina = 20, int? vendaId = null)
    {
        const string from = @"
            FROM devolucoes d
            JOIN vendas v ON v.id = d.venda_id
            JOIN clientes c ON c.id = v.cliente_id
            WHERE (unaccent(d.id::text) ILIKE unaccent(@Termo::text) OR unaccent(d.venda_id::text) ILIKE unaccent(@Termo::text)
                   OR unaccent(c.nome_razaosocial::text) ILIKE unaccent(@Termo::text) OR unaccent(d.motivo::text) ILIKE unaccent(@Termo::text))
              AND (@VendaId::int IS NULL OR d.venda_id = @VendaId::int)";

        var param = new { Termo = $"%{termo}%", VendaId = vendaId, TamanhoDaPagina = tamanhoDaPagina, Offset = (pagina - 1) * tamanhoDaPagina };

        var total = await _session.Connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) {from};", param, transaction: _session.Transaction);

        var itens = (await _session.Connection.QueryAsync<DevolucaoResumo>(
            $@"SELECT d.id AS Id, d.venda_id AS VendaId, c.nome_razaosocial AS ClienteNome, d.data_devolucao AS DataDevolucao,
                      d.valor_total AS ValorTotal, d.motivo AS Motivo, d.status AS Status
               {from}
               ORDER BY d.data_devolucao DESC, d.id DESC
               LIMIT @TamanhoDaPagina OFFSET @Offset;",
            param, transaction: _session.Transaction)).ToList();

        return new ResultadoPaginado<DevolucaoResumo>(itens, total, pagina, tamanhoDaPagina);
    }

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

    public async Task<bool> MarcarEstornada(Devolucao devolucao)
        => await _session.Connection.ExecuteAsync(
            @"UPDATE devolucoes SET status = 'ESTORNADA', estornado_em = @EstornadoEm, motivo_estorno = @MotivoEstorno
              WHERE id = @Id AND status = 'ATIVA';",
            new { devolucao.Id, devolucao.EstornadoEm, devolucao.MotivoEstorno }, transaction: _session.Transaction) > 0;

    public async Task CriarAbatimento(DevolucaoAbatimento a)
        => await _session.Connection.ExecuteAsync(
            "INSERT INTO devolucao_abatimentos (devolucao_id, conta_receber_id, valor) VALUES (@DevolucaoId, @ContaReceberId, @Valor);",
            a, transaction: _session.Transaction);

    public async Task<IReadOnlyList<DevolucaoAbatimento>> ObterAbatimentosAtivos(int devolucaoId)
        => (await _session.Connection.QueryAsync<AbatimentoRow>(
            @"SELECT id, devolucao_id AS DevolucaoId, conta_receber_id AS ContaReceberId, valor AS Valor, estornado_em AS EstornadoEm
              FROM devolucao_abatimentos WHERE devolucao_id = @DevolucaoId AND estornado_em IS NULL ORDER BY id;",
            new { DevolucaoId = devolucaoId }, transaction: _session.Transaction))
            .Select(r => new DevolucaoAbatimento(r.Id, r.DevolucaoId, r.ContaReceberId, r.Valor, r.EstornadoEm)).ToList();

    public async Task MarcarAbatimentosEstornados(int devolucaoId, DateTime estornadoEm)
        => await _session.Connection.ExecuteAsync(
            "UPDATE devolucao_abatimentos SET estornado_em = @EstornadoEm WHERE devolucao_id = @DevolucaoId AND estornado_em IS NULL;",
            new { DevolucaoId = devolucaoId, EstornadoEm = estornadoEm }, transaction: _session.Transaction);

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
            var d = new Devolucao(r.Id, r.VendaId, r.DataDevolucao, r.Motivo, r.ValorTotal, Enum.Parse<StatusDevolucao>(r.Status), r.EstornadoEm, r.MotivoEstorno);
            foreach (var i in itens[r.Id])
                d.AdicionarItemExistente(new DevolucaoItens(i.Id, i.VendaItemId, i.Sku, i.Quantidade, i.ValorUnitario, i.CustoUnitario));
            return d;
        }).ToList();
    }

    private sealed record DevolucaoRow(int Id, int VendaId, DateTime DataDevolucao, string Motivo, decimal ValorTotal, string Status, DateTime? EstornadoEm, string? MotivoEstorno);

    private sealed record AbatimentoRow(int Id, int DevolucaoId, int ContaReceberId, decimal Valor, DateTime? EstornadoEm);

    private sealed record ItemRow(int Id, int DevolucaoId, int VendaItemId, string Sku, decimal Quantidade, decimal ValorUnitario, decimal CustoUnitario);
}
