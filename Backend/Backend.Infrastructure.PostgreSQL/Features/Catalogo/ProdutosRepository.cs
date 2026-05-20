using Backend.Core.Common;
using Backend.Core.Features.Catalogo.DTOs;
using Backend.Core.Features.Catalogo.Entities;
using Backend.Core.Features.Catalogo.Repositories;
using Backend.Core.Features.UnidadeMedida.Entities;
using Backend.Infrastructure.PostgreSQL.Common;
using Dapper;

namespace Backend.Infrastructure.PostgreSQL.Features.Catalogo;

public class ProdutosRepository : IProdutosRepository
{
    private readonly DbSession _session;

    public ProdutosRepository(DbSession session)
    {
        _session = session;
    }

    public async Task<ResultadoPaginado<Produtos>> ObterProdutos(int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string countSql = "SELECT COUNT(*) FROM produtos;";

        const string querySql = @"
            SELECT p.id AS Id, p.produto, p.descricao, p.ativo,
                   c.id AS CategoriaId, c.categoria, c.descricao, c.ativo,
                   m.id AS MarcaId, m.marca, m.descricao, m.ativo,
                   u.id AS UnidadeMedidaId, u.sigla, u.descricao, u.categoria, u.ativo
            FROM produtos p
            JOIN categorias c ON c.id = p.categoria_id
            JOIN marcas m ON m.id = p.marca_id
            JOIN unidades_medida u ON u.id = p.unidade_medida_id
            ORDER BY p.produto
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        var total = await _session.Connection.ExecuteScalarAsync<int>(
            countSql, transaction: _session.Transaction);

        var itens = (await _session.Connection.QueryAsync<Produtos, Categorias, Marcas, UnidadesMedida, Produtos>(
            querySql,
            (produto, categoria, marca, unidadeMedida) =>
            {
                produto.Categoria = categoria;
                produto.Marca = marca;
                produto.UnidadeMedida = unidadeMedida;
                produto.Skus = [];
                return produto;
            },
            new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction,
            splitOn: "CategoriaId,MarcaId,UnidadeMedidaId"
        )).ToList();

        return new ResultadoPaginado<Produtos>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<Produtos?> ObterProdutoPorId(int id)
    {
        const string produtoSql = @"
            SELECT p.id AS Id, p.produto, p.descricao, p.ativo,
                   c.id AS CategoriaId, c.categoria, c.descricao, c.ativo,
                   m.id AS MarcaId, m.marca, m.descricao, m.ativo,
                   u.id AS UnidadeMedidaId, u.sigla, u.descricao, u.categoria, u.ativo
            FROM produtos p
            JOIN categorias c ON c.id = p.categoria_id
            JOIN marcas m ON m.id = p.marca_id
            JOIN unidades_medida u ON u.id = p.unidade_medida_id
            WHERE p.id = @Id;";

        const string skusSql = @"
            SELECT sku, gtin_ean, preco, estoque, ativo
            FROM skus
            WHERE produto_id = @Id;";

        var produto = (await _session.Connection.QueryAsync<Produtos, Categorias, Marcas, UnidadesMedida, Produtos>(
            produtoSql,
            (p, categoria, marca, unidadeMedida) =>
            {
                p.Categoria = categoria;
                p.Marca = marca;
                p.UnidadeMedida = unidadeMedida;
                p.Skus = [];
                return p;
            },
            new { Id = id },
            transaction: _session.Transaction,
            splitOn: "CategoriaId,MarcaId,UnidadeMedidaId"
        )).SingleOrDefault();

        if (produto is null) return null;

        var skus = await _session.Connection.QueryAsync<Skus>(
            skusSql, new { Id = id }, transaction: _session.Transaction);

        foreach (var sku in skus) sku.SkusAtributosValores = [];
        produto.Skus = skus;

        return produto;
    }

    public async Task<Produtos?> ObterProdutoPorSku(string sku)
    {
        const string sql = @"
            SELECT p.id AS Id, p.produto, p.descricao, p.ativo,
                   c.id AS CategoriaId, c.categoria, c.descricao, c.ativo,
                   m.id AS MarcaId, m.marca, m.descricao, m.ativo,
                   u.id AS UnidadeMedidaId, u.sigla, u.descricao, u.categoria, u.ativo
            FROM produtos p
            JOIN categorias c ON c.id = p.categoria_id
            JOIN marcas m ON m.id = p.marca_id
            JOIN unidades_medida u ON u.id = p.unidade_medida_id
            JOIN skus s ON s.produto_id = p.id
            WHERE s.sku = @Sku;";

        var result = await _session.Connection.QueryAsync<Produtos, Categorias, Marcas, UnidadesMedida, Produtos>(
            sql,
            (p, categoria, marca, unidadeMedida) =>
            {
                p.Categoria = categoria;
                p.Marca = marca;
                p.UnidadeMedida = unidadeMedida;
                p.Skus = [];
                return p;
            },
            new { Sku = sku },
            transaction: _session.Transaction,
            splitOn: "CategoriaId,MarcaId,UnidadeMedidaId"
        );

        return result.SingleOrDefault();
    }

    public async Task<Produtos> CriarProduto(Produtos produto)
    {
        const string sql = @"
            INSERT INTO produtos (produto, descricao, ativo, categoria_id, marca_id, unidade_medida_id)
            VALUES (@Produto, @Descricao, @Ativo, @CategoriaId, @MarcaId, @UnidadeMedidaId)
            RETURNING id;";

        var idGerado = await _session.Connection.ExecuteScalarAsync<int>(
            sql,
            new
            {
                produto.Produto,
                produto.Descricao,
                produto.Ativo,
                CategoriaId = produto.Categoria.Id,
                MarcaId = produto.Marca.Id,
                UnidadeMedidaId = produto.UnidadeMedida.Id
            },
            transaction: _session.Transaction
        );

        produto.Id = idGerado;
        return produto;
    }

    public async Task<Produtos> AtualizarProduto(int id, Produtos produto)
    {
        const string sql = @"
            UPDATE produtos
            SET produto = @Produto,
                descricao = @Descricao,
                ativo = @Ativo,
                categoria_id = @CategoriaId,
                marca_id = @MarcaId,
                unidade_medida_id = @UnidadeMedidaId
            WHERE id = @Id;";

        await _session.Connection.ExecuteAsync(
            sql,
            new
            {
                Id = id,
                produto.Produto,
                produto.Descricao,
                produto.Ativo,
                CategoriaId = produto.Categoria.Id,
                MarcaId = produto.Marca.Id,
                UnidadeMedidaId = produto.UnidadeMedida.Id
            },
            transaction: _session.Transaction
        );

        produto.Id = id;
        return produto;
    }

    public async Task<bool> DeletarProduto(int id)
    {
        const string sql = "DELETE FROM produtos WHERE id = @Id;";

        var linhasAfetadas = await _session.Connection.ExecuteAsync(
            sql,
            new { Id = id },
            transaction: _session.Transaction
        );

        return linhasAfetadas > 0;
    }

    public async Task<ResultadoPaginado<ProdutosResumo>> ObterProdutosResumo(int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sql = @"
            SELECT COUNT(*) FROM produtos;

            SELECT id, produto, descricao, ativo
            FROM produtos
            ORDER BY produto
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql,
            new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction
        );

        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<ProdutosResumo>();

        return new ResultadoPaginado<ProdutosResumo>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<ResultadoPaginado<ProdutosResumo>> PesquisarProdutos(string termo, int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sql = @"
            SELECT COUNT(*)
            FROM produtos
            WHERE produto ILIKE @Termo OR descricao ILIKE @Termo;

            SELECT id, produto, descricao, ativo
            FROM produtos
            WHERE produto ILIKE @Termo OR descricao ILIKE @Termo
            ORDER BY produto
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql,
            new { Termo = $"%{termo}%", TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction
        );

        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<ProdutosResumo>();

        return new ResultadoPaginado<ProdutosResumo>(itens, total, pagina, tamanhoDaPagina);
    }
}
