using Backend.Core.Features.Relacionados;
using Backend.Infrastructure.PostgreSQL.Common;
using Dapper;

namespace Backend.Infrastructure.PostgreSQL.Features.Relacionados;

public class RelacionadosRepository : IRelacionadosRepository
{
    private readonly DbSession _session;

    public RelacionadosRepository(DbSession session) => _session = session;

    private async Task<IReadOnlyList<RelacionadoItem>> Consultar(string sql, object param)
        => (await _session.Connection.QueryAsync<RelacionadoItem>(sql, param, _session.Transaction)).ToList();

    public Task<IReadOnlyList<RelacionadoItem>> ContasReceberPorVenda(int vendaId) => Consultar(
        "SELECT id AS Id, descricao AS Descricao, status::text AS Detalhe FROM contas_receber WHERE venda_id = @Id ORDER BY id;",
        new { Id = vendaId });

    public Task<IReadOnlyList<RelacionadoItem>> MovimentacoesPorVenda(int vendaId) => Consultar(
        "SELECT id AS Id, 'Movimentação #' || id AS Descricao, status::text AS Detalhe FROM movimentacoes_estoque WHERE venda_id = @Id ORDER BY id;",
        new { Id = vendaId });

    public Task<IReadOnlyList<RelacionadoItem>> ContasReceberPorCliente(int clienteId) => Consultar(
        "SELECT id AS Id, descricao AS Descricao, status::text AS Detalhe FROM contas_receber WHERE cliente_id = @Id ORDER BY id DESC;",
        new { Id = clienteId });

    public Task<IReadOnlyList<RelacionadoItem>> ContasPagarPorFornecedor(int fornecedorId) => Consultar(
        "SELECT id AS Id, descricao AS Descricao, status::text AS Detalhe FROM contas_pagar WHERE fornecedor_id = @Id ORDER BY id DESC;",
        new { Id = fornecedorId });

    public Task<IReadOnlyList<RelacionadoItem>> VendasPorCliente(int clienteId) => Consultar(
        "SELECT id AS Id, 'Venda #' || id AS Descricao, to_char(valor_total, 'FM999G999G990D00') AS Detalhe FROM vendas WHERE cliente_id = @Id ORDER BY id DESC;",
        new { Id = clienteId });

    public Task<IReadOnlyList<RelacionadoItem>> ProdutosPor(string campo, int id)
    {
        // Lista branca: o nome da coluna nunca vem do cliente.
        var coluna = campo switch
        {
            "categoriaId" => "categoria_id",
            "marcaId" => "marca_id",
            "unidadeMedidaId" => "unidade_medida_id",
            _ => throw new ArgumentException("Filtro de produto inválido."),
        };
        return Consultar($"SELECT id AS Id, produto AS Descricao, NULL::text AS Detalhe FROM produtos WHERE {coluna} = @Id ORDER BY produto;", new { Id = id });
    }

    public Task<IReadOnlyList<RelacionadoItem>> LocalizacaoFilhos(string pai, int id)
    {
        var sql = pai switch
        {
            "pais" => "SELECT id AS Id, estado AS Descricao, uf AS Detalhe FROM estados WHERE pais_id = @Id ORDER BY estado;",
            "estado" => "SELECT id AS Id, cidade AS Descricao, ddd AS Detalhe FROM cidades WHERE estado_id = @Id ORDER BY cidade;",
            "cidade" => "SELECT id AS Id, bairro AS Descricao, NULL::text AS Detalhe FROM bairros WHERE cidade_id = @Id ORDER BY bairro;",
            _ => throw new ArgumentException("Pai de localização inválido."),
        };
        return Consultar(sql, new { Id = id });
    }
}
