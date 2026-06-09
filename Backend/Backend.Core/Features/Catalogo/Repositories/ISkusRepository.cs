using Backend.Core.Common.Results;
using Backend.Core.Features.Catalogo.Entities;

namespace Backend.Core.Features.Catalogo.Repositories;

public interface ISkusRepository
{
    public Task<ResultadoPaginado<Skus>> ObterSkus(int pagina = 1, int tamanhoDaPagina = 20);
    public Task<ResultadoPaginado<Skus>> ObterSkusPorProduto(int produtoId);
    public Task<Skus?> ObterSkuPorSku(string sku);
    public Task<Skus> CriarSku(int produtoId, Skus skuData);
    public Task<Skus> AtualizarSku(string sku, Skus skuData);
    public Task<bool> DeletarSku(string sku);
    public Task<ResultadoPaginado<Skus>> PesquisarSkus(string termo, int pagina = 1, int tamanhoDaPagina = 20);
    public Task<Skus?> ObterSkuCompleto(string sku);
    public Task<Produtos?> ObterProdutoPorSku(string sku);
}
