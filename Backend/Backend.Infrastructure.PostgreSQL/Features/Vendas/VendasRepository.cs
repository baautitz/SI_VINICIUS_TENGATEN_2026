using Backend.Core.Common.Results;
using Backend.Core.Features.Catalogo.Entities;
using Backend.Core.Features.Parceiros.Entities;
using Backend.Core.Features.Vendas.DTOs;
using Backend.Core.Features.Vendas.Entities;
using Backend.Core.Features.Vendas.Repositories;
using Backend.Infrastructure.PostgreSQL.Common;
using Dapper;

namespace Backend.Infrastructure.PostgreSQL.Features.Vendas;

public class VendasRepository : IVendasRepository
{
    private readonly DbSession _session;

    public VendasRepository(DbSession session)
    {
        _session = session;
    }

    public async Task<ResultadoPaginado<Venda>> ObterVendas(int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string countSql = "SELECT COUNT(*) FROM vendas;";

        const string querySql = @"
            SELECT v.id AS Id, v.data_venda, v.valor_total, v.observacao,
                   e.id AS EmitenteId, e.nome_razao_social, e.cpf_cnpj, e.ativo,
                   c.id AS ClienteId, c.nome_razao_social, c.cpf_cnpj, c.ativo
            FROM vendas v
            JOIN emitentes e ON e.id = v.emitente_id
            JOIN clientes c ON c.id = v.cliente_id
            ORDER BY v.data_venda DESC
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        var total = await _session.Connection.ExecuteScalarAsync<int>(
            countSql, transaction: _session.Transaction);

        var vendas = (await _session.Connection.QueryAsync<Venda, Emitentes, Clientes, Venda>(
            querySql,
            (venda, emitente, cliente) =>
            {
                venda.AtualizarEmitente(emitente);
                venda.AtualizarCliente(cliente);
                venda.DefinirItens(new List<VendaItens>());
                return venda;
            },
            new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction,
            splitOn: "EmitenteId,ClienteId")).ToList();

        if (vendas.Count > 0)
        {
            var ids = vendas.Select(v => v.Id).ToArray();

            const string itensSql = @"
                SELECT vi.id, vi.venda_id, vi.quantidade, vi.valor_unitario, vi.valor_desconto, vi.valor_total,
                       s.sku, s.gtin_ean, s.preco, s.estoque, s.ativo
                FROM vendas_itens vi
                JOIN skus s ON s.sku = vi.sku
                WHERE vi.venda_id = ANY(@Ids)
                ORDER BY vi.venda_id, vi.id;";

            var itens = (await _session.Connection.QueryAsync<VendaItens, Skus, VendaItens>(
                itensSql,
                (item, sku) =>
                {
                    sku.DefinirAtributos(new List<SkusAtributosValores>());
                    item.AtualizarSku(sku);
                    return item;
                },
                new { Ids = ids },
                transaction: _session.Transaction,
                splitOn: "sku")).ToList();

            var itensPorVenda = itens
                .GroupBy(i => i.VendaId)
                .ToDictionary(g => g.Key, g => g.AsEnumerable());

            foreach (var venda in vendas)
            {
                if (itensPorVenda.TryGetValue(venda.Id, out var sub))
                    venda.DefinirItens(sub.ToList());
            }
        }

        return new ResultadoPaginado<Venda>(vendas, total, pagina, tamanhoDaPagina);
    }

    public async Task<Venda?> ObterVendaPorId(int id)
    {
        const string vendaSql = @"
            SELECT v.id AS Id, v.data_venda, v.valor_total, v.observacao,
                   e.id AS EmitenteId, e.nome_razao_social, e.cpf_cnpj, e.apelido_nome_fantasia,
                   e.endereco, e.telefone, e.email, e.rg_ie, e.inscricao_municipal,
                   e.regime_tributario, e.ativo, e.criado_em, e.observacao,
                   c.id AS ClienteId, c.nome_razao_social, c.cpf_cnpj, c.rg_ie, c.apelido_nome_fantasia,
                   c.endereco, c.telefone, c.email, c.limite_credito, c.ativo, c.criado_em,
                   c.observacao
            FROM vendas v
            JOIN emitentes e ON e.id = v.emitente_id
            JOIN clientes c ON c.id = v.cliente_id
            WHERE v.id = @Id;";

        const string itensSql = @"
            SELECT vi.id, vi.quantidade, vi.valor_unitario, vi.valor_desconto, vi.valor_total,
                   s.sku, s.gtin_ean, s.preco, s.estoque, s.ativo
            FROM vendas_itens vi
            JOIN skus s ON s.sku = vi.sku
            WHERE vi.venda_id = @Id
            ORDER BY vi.id;";

        var venda = (await _session.Connection.QueryAsync<Venda, Emitentes, Clientes, Venda>(
            vendaSql,
            (v, emitente, cliente) =>
            {
                v.AtualizarEmitente(emitente);
                v.AtualizarCliente(cliente);
                v.DefinirItens(new List<VendaItens>());
                return v;
            },
            new { Id = id },
            transaction: _session.Transaction,
            splitOn: "EmitenteId,ClienteId")).SingleOrDefault();

        if (venda is null) return null;

        var vendaItens = await _session.Connection.QueryAsync<VendaItens, Skus, VendaItens>(
            itensSql,
            (item, sku) =>
            {
                sku.DefinirAtributos(new List<SkusAtributosValores>());
                item.AtualizarSku(sku);
                return item;
            },
            new { Id = id },
            transaction: _session.Transaction,
            splitOn: "sku");

        venda.DefinirItens(vendaItens.ToList());
        return venda;
    }

    public async Task<Venda> CriarVenda(Venda venda)
    {
        const string sql = @"
            INSERT INTO vendas (data_venda, valor_total, observacao, emitente_id, cliente_id)
            VALUES (@DataVenda, @ValorTotal, @Observacao, @EmitenteId, @ClienteId)
            RETURNING id;";

        var idGerado = await _session.Connection.ExecuteScalarAsync<int>(
            sql,
            new
            {
                venda.DataVenda,
                venda.ValorTotal,
                venda.Observacao,
                EmitenteId = venda.Emitente.Id,
                ClienteId = venda.Cliente.Id
            },
            transaction: _session.Transaction);

        venda.Id = idGerado;
        await InserirItens(idGerado, venda.Itens);

        return venda;
    }

    public async Task<Venda> AtualizarVenda(int id, Venda venda)
    {
        const string sql = @"
            UPDATE vendas
            SET data_venda = @DataVenda, valor_total = @ValorTotal,
                observacao = @Observacao, emitente_id = @EmitenteId, cliente_id = @ClienteId
            WHERE id = @Id;";

        await _session.Connection.ExecuteAsync(
            sql,
            new
            {
                Id = id,
                venda.DataVenda,
                venda.ValorTotal,
                venda.Observacao,
                EmitenteId = venda.Emitente.Id,
                ClienteId = venda.Cliente.Id
            },
            transaction: _session.Transaction);

        await ReplacerItens(id, venda.Itens);

        venda.Id = id;
        return venda;
    }

    public async Task<bool> DeletarVenda(int id)
    {
        await _session.Connection.ExecuteAsync(
            "DELETE FROM vendas_itens WHERE venda_id = @Id;",
            new { Id = id }, transaction: _session.Transaction);

        var linhasAfetadas = await _session.Connection.ExecuteAsync(
            "DELETE FROM vendas WHERE id = @Id;",
            new { Id = id }, transaction: _session.Transaction);

        return linhasAfetadas > 0;
    }

    public async Task<ResultadoPaginado<VendasResumo>> ObterVendasResumo(int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sql = @"
            SELECT COUNT(*) FROM vendas;

            SELECT v.id, v.data_venda, c.nome_razao_social AS cliente_nome, v.valor_total
            FROM vendas v
            JOIN clientes c ON c.id = v.cliente_id
            ORDER BY v.data_venda DESC
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql, new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction);

        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<VendasResumo>();

        return new ResultadoPaginado<VendasResumo>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<ResultadoPaginado<VendasResumo>> PesquisarVendas(string termo, int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sql = @"
            SELECT COUNT(*) FROM vendas v
            JOIN clientes c ON c.id = v.cliente_id
            WHERE c.nome_razao_social ILIKE @Termo OR v.observacao ILIKE @Termo;

            SELECT v.id, v.data_venda, c.nome_razao_social AS cliente_nome, v.valor_total
            FROM vendas v
            JOIN clientes c ON c.id = v.cliente_id
            WHERE c.nome_razao_social ILIKE @Termo OR v.observacao ILIKE @Termo
            ORDER BY v.data_venda DESC
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql,
            new { Termo = $"%{termo}%", TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction);

        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<VendasResumo>();

        return new ResultadoPaginado<VendasResumo>(itens, total, pagina, tamanhoDaPagina);
    }

    private async Task InserirItens(int vendaId, IEnumerable<VendaItens> itens)
    {
        const string sql = @"
            INSERT INTO vendas_itens (quantidade, valor_unitario, valor_desconto, valor_total, sku, venda_id)
            VALUES (@Quantidade, @ValorUnitario, @ValorDesconto, @ValorTotal, @SkuCodigo, @VendaId);";

        await _session.Connection.ExecuteAsync(
            sql,
            itens.Select(i => new
            {
                i.Quantidade,
                i.ValorUnitario,
                i.ValorDesconto,
                i.ValorTotal,
                SkuCodigo = i.Sku.Sku,
                VendaId = vendaId
            }),
            transaction: _session.Transaction);
    }

    private async Task ReplacerItens(int vendaId, IEnumerable<VendaItens> itens)
    {
        await _session.Connection.ExecuteAsync(
            "DELETE FROM vendas_itens WHERE venda_id = @VendaId;",
            new { VendaId = vendaId }, transaction: _session.Transaction);

        await InserirItens(vendaId, itens);
    }
}
