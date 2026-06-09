using System.Linq;
using Backend.Core.Common.Results;
using Backend.Core.Features.Catalogo.Entities;
using Backend.Core.Features.Catalogo.Repositories;
using Backend.Infrastructure.PostgreSQL.Common;
using Dapper;
using Npgsql;

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
            SELECT p.id AS Id, p.produto AS Produto, p.descricao AS Descricao, p.ativo AS Ativo,
                   c.id AS CategoriaId, c.categoria AS CategoriaNome, c.descricao AS CategoriaDescricao, c.ativo AS CategoriaAtivo,
                   m.id AS MarcaId, m.marca AS MarcaNome, m.descricao AS MarcaDescricao, m.ativo AS MarcaAtivo,
                   u.id AS UnidadeMedidaId, u.sigla AS UnidadeMedidaSigla, u.descricao AS UnidadeMedidaDescricao, u.categoria AS UnidadeMedidaCategoria, u.ativo AS UnidadeMedidaAtivo
            FROM produtos p
            JOIN categorias c ON c.id = p.categoria_id
            JOIN marcas m ON m.id = p.marca_id
            JOIN unidades_medida u ON u.id = p.unidade_medida_id
            ORDER BY p.produto
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        var total = await _session.Connection.ExecuteScalarAsync<int>(
            countSql, transaction: _session.Transaction);

        var produtosDto = (await _session.Connection.QueryAsync<ProdutoDto>(
            querySql,
            new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction)).ToList();

        var produtos = produtosDto.Select(p => BuildProdutoFromDto(p)).ToList();

        // Carregar SKUs
        if (produtos.Any())
        {
            var ids = produtos.Select(p => p.Id).ToArray();
            const string skusSql = @"
                SELECT sku, gtin_ean, preco, estoque, ativo, produto_id AS ProdutoId
                FROM skus
                WHERE produto_id = ANY(@Ids);";

            var allSkus = (await _session.Connection.QueryAsync<SkuDto>(
                skusSql, new { Ids = ids }, transaction: _session.Transaction)).ToList();

            var skuCodes = allSkus.Select(s => s.Sku).ToArray();
            const string atributosSql = @"
                SELECT savr.sku, sav.id AS Id, sav.chave_id AS ChaveId, sav.valor AS Valor
                FROM skus_atributos_valores_relacionamento savr
                JOIN sku_atributos_valores sav ON sav.id = savr.valor_id
                WHERE savr.sku = ANY(@Skus);";

            var atributos = (await _session.Connection.QueryAsync<SkuAtributoDto>(
                atributosSql, new { Skus = skuCodes }, transaction: _session.Transaction)).ToList();

            var atributosPorSku = atributos.GroupBy(a => a.Sku).ToDictionary(g => g.Key, g => g.AsEnumerable());

            foreach (var produto in produtos)
            {
                var skus = allSkus.Where(s => s.ProdutoId == produto.Id);
                foreach (var skuDto in skus)
                {
                    produto.AdicionarSku(BuildSkuFromDto(skuDto, atributosPorSku.GetValueOrDefault(skuDto.Sku, Enumerable.Empty<SkuAtributoDto>())));
                }
            }
        }

        return new ResultadoPaginado<Produtos>(produtos, total, pagina, tamanhoDaPagina);
    }

    public async Task<Produtos?> ObterProdutoPorId(int id)
    {
        const string produtoSql = @"
            SELECT p.id AS Id, p.produto AS Produto, p.descricao AS Descricao, p.ativo AS Ativo,
                   c.id AS CategoriaId, c.categoria AS CategoriaNome, c.descricao AS CategoriaDescricao, c.ativo AS CategoriaAtivo,
                   m.id AS MarcaId, m.marca AS MarcaNome, m.descricao AS MarcaDescricao, m.ativo AS MarcaAtivo,
                   u.id AS UnidadeMedidaId, u.sigla AS UnidadeMedidaSigla, u.descricao AS UnidadeMedidaDescricao, u.categoria AS UnidadeMedidaCategoria, u.ativo AS UnidadeMedidaAtivo
            FROM produtos p
            JOIN categorias c ON c.id = p.categoria_id
            JOIN marcas m ON m.id = p.marca_id
            JOIN unidades_medida u ON u.id = p.unidade_medida_id
            WHERE p.id = @Id;";

        const string skusSql = @"
            SELECT sku, gtin_ean, preco, estoque, ativo, produto_id AS ProdutoId
            FROM skus
            WHERE produto_id = @Id;";

        var produtoDto = await _session.Connection.QuerySingleOrDefaultAsync<ProdutoDto>(
            produtoSql,
            new { Id = id },
            transaction: _session.Transaction);

        if (produtoDto is null) return null;

        var produto = BuildProdutoFromDto(produtoDto);

        var skus = await _session.Connection.QueryAsync<SkuDto>(
            skusSql, new { Id = id }, transaction: _session.Transaction);

        if (skus.Any())
        {
            var skuCodes = skus.Select(s => s.Sku).ToArray();
            const string atributosSql = @"
                SELECT savr.sku, sav.id AS Id, sav.chave_id AS ChaveId, sav.valor AS Valor
                FROM skus_atributos_valores_relacionamento savr
                JOIN sku_atributos_valores sav ON sav.id = savr.valor_id
                WHERE savr.sku = ANY(@Skus);";

            var atributos = (await _session.Connection.QueryAsync<SkuAtributoDto>(
                atributosSql,
                new { Skus = skuCodes },
                transaction: _session.Transaction)).ToList();

            var atributosPorSku = atributos.GroupBy(a => a.Sku)
                .ToDictionary(g => g.Key, g => g.AsEnumerable());

            foreach (var skuDto in skus)
            {
                var sku = BuildSkuFromDto(skuDto, atributosPorSku.GetValueOrDefault(skuDto.Sku, Enumerable.Empty<SkuAtributoDto>()));
                produto.AdicionarSku(sku);
            }
        }

        return produto;
    }

    public async Task<Produtos?> ObterProdutoPorSku(string sku)
    {
        const string sql = @"
            SELECT p.id AS Id, p.produto AS Produto, p.descricao AS Descricao, p.ativo AS Ativo,
                   c.id AS CategoriaId, c.categoria AS CategoriaNome, c.descricao AS CategoriaDescricao, c.ativo AS CategoriaAtivo,
                   m.id AS MarcaId, m.marca AS MarcaNome, m.descricao AS MarcaDescricao, m.ativo AS MarcaAtivo,
                   u.id AS UnidadeMedidaId, u.sigla AS UnidadeMedidaSigla, u.descricao AS UnidadeMedidaDescricao, u.categoria AS UnidadeMedidaCategoria, u.ativo AS UnidadeMedidaAtivo
            FROM produtos p
            JOIN categorias c ON c.id = p.categoria_id
            JOIN marcas m ON m.id = p.marca_id
            JOIN unidades_medida u ON u.id = p.unidade_medida_id
            JOIN skus s ON s.produto_id = p.id
            WHERE s.sku = @Sku;";

        var produtoDto = await _session.Connection.QuerySingleOrDefaultAsync<ProdutoDto>(
            sql,
            new { Sku = sku },
            transaction: _session.Transaction);

        if (produtoDto is null)
            return null;

        return await ObterProdutoPorId(produtoDto.Id);
    }
    public async Task<Produtos> CriarProduto(Produtos produto)
    {
        try
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

            var produtoCriado = new Produtos(idGerado, produto.Produto, produto.Descricao, produto.Categoria, produto.Marca, produto.UnidadeMedida);
            if (!produto.Ativo)
                produtoCriado.Desativar();

            return produtoCriado;
        }
        catch (PostgresException ex)
        {
            throw DbExceptionTranslator.Translate(ex);
        }
    }

    public async Task<Produtos> AtualizarProduto(int id, Produtos produto)
    {
        try
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

            return produto;
        }
        catch (PostgresException ex)
        {
            throw DbExceptionTranslator.Translate(ex);
        }
    }

    public async Task<bool> DeletarProduto(int id)
    {
        try
        {
            await _session.Connection.ExecuteAsync(
                "DELETE FROM skus WHERE produto_id = @Id;",
                new { Id = id }, transaction: _session.Transaction);

            const string sql = "DELETE FROM produtos WHERE id = @Id;";

            var linhasAfetadas = await _session.Connection.ExecuteAsync(
                sql,
                new { Id = id },
                transaction: _session.Transaction
            );

            return linhasAfetadas > 0;
        }
        catch (PostgresException ex)
        {
            throw DbExceptionTranslator.Translate(ex);
        }
    }

    public async Task<ResultadoPaginado<Produtos>> PesquisarProdutos(string termo, int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string countSql = @"
            SELECT COUNT(*)
            FROM produtos
            WHERE produto ILIKE @Termo OR descricao ILIKE @Termo;";

        const string querySql = @"
            SELECT p.id AS Id, p.produto AS Produto, p.descricao AS Descricao, p.ativo AS Ativo,
                   c.id AS CategoriaId, c.categoria AS CategoriaNome, c.descricao AS CategoriaDescricao, c.ativo AS CategoriaAtivo,
                   m.id AS MarcaId, m.marca AS MarcaNome, m.descricao AS MarcaDescricao, m.ativo AS MarcaAtivo,
                   u.id AS UnidadeMedidaId, u.sigla AS UnidadeMedidaSigla, u.descricao AS UnidadeMedidaDescricao, u.categoria AS UnidadeMedidaCategoria, u.ativo AS UnidadeMedidaAtivo
            FROM produtos p
            JOIN categorias c ON c.id = p.categoria_id
            JOIN marcas m ON m.id = p.marca_id
            JOIN unidades_medida u ON u.id = p.unidade_medida_id
            WHERE p.produto ILIKE @Termo OR p.descricao ILIKE @Termo
            ORDER BY p.produto
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        var total = await _session.Connection.ExecuteScalarAsync<int>(
            countSql, new { Termo = $"%{termo}%" }, transaction: _session.Transaction);

        var produtosDto = (await _session.Connection.QueryAsync<ProdutoDto>(
            querySql,
            new { Termo = $"%{termo}%", TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction)).ToList();

        var produtos = produtosDto.Select(p => BuildProdutoFromDto(p)).ToList();

        // ... Load Skus similarly to ObterProdutos ...
        return new ResultadoPaginado<Produtos>(produtos, total, pagina, tamanhoDaPagina);
    }

    public async Task<bool> ExisteProduto(string produto, int? ignorarId = null)
    {
        var sql = "SELECT COUNT(1) FROM produtos WHERE unaccent(produto::text) ILIKE unaccent(@Produto::text)";
        if (ignorarId.HasValue)
            sql += " AND id != @IgnorarId";

        return await _session.Connection.ExecuteScalarAsync<int>(
            sql,
            new { Produto = produto, IgnorarId = ignorarId },
            transaction: _session.Transaction
        ) > 0;
    }

    private static Produtos BuildProdutoFromDto(ProdutoDto dto)
    {
        var categoria = new Categorias(dto.CategoriaId, dto.CategoriaNome, dto.CategoriaDescricao);
        if (!dto.CategoriaAtivo)
            categoria.Desativar();

        var marca = new Marcas(dto.MarcaId, dto.MarcaNome, dto.MarcaDescricao);
        if (!dto.MarcaAtivo)
            marca.Desativar();

        var unidadeMedida = new UnidadesMedida(
            dto.UnidadeMedidaSigla,
            dto.UnidadeMedidaDescricao,
            dto.UnidadeMedidaCategoria,
            dto.UnidadeMedidaAtivo)
        {
            Id = dto.UnidadeMedidaId
        };

        var produto = new Produtos(dto.Id, dto.Produto, dto.Descricao, categoria, marca, unidadeMedida);
        if (!dto.Ativo)
            produto.Desativar();

        return produto;
    }

    private static Skus BuildSkuFromDto(SkuDto dto, IEnumerable<SkuAtributoDto> atributos)
    {
        var sku = new Skus(dto.Sku, dto.Preco, dto.Estoque, dto.GtinEan);

        if (!dto.Ativo)
            sku.Desativar();

        foreach (var atributo in atributos)
            sku.AdicionarAtributo(new SkuAtributosValores(atributo.Id, atributo.ChaveId, atributo.Valor));

        return sku;
    }

    private sealed class ProdutoDto
    {
        public int Id { get; set; }
        public string Produto { get; set; } = null!;
        public string Descricao { get; set; } = null!;
        public bool Ativo { get; set; }

        public int CategoriaId { get; set; }
        public string CategoriaNome { get; set; } = null!;
        public string? CategoriaDescricao { get; set; }
        public bool CategoriaAtivo { get; set; }

        public int MarcaId { get; set; }
        public string MarcaNome { get; set; } = null!;
        public string? MarcaDescricao { get; set; }
        public bool MarcaAtivo { get; set; }

        public int UnidadeMedidaId { get; set; }
        public string UnidadeMedidaSigla { get; set; } = null!;
        public string UnidadeMedidaDescricao { get; set; } = null!;
        public string UnidadeMedidaCategoria { get; set; } = null!;
        public bool UnidadeMedidaAtivo { get; set; }
    }

    private sealed class SkuDto
    {
        public string Sku { get; set; } = null!;
        public string? GtinEan { get; set; }
        public decimal Preco { get; set; }
        public decimal Estoque { get; set; }
        public bool Ativo { get; set; }
        public int ProdutoId { get; set; }
    }

    private sealed class SkuAtributoDto
    {
        public string Sku { get; set; } = null!;
        public int Id { get; set; }
        public int ChaveId { get; set; }
        public string Valor { get; set; } = null!;
    }
}
