using backend.Modules.Catalogo.DTOs;
using backend.Modules.Catalogo.Models;

namespace backend.Modules.Catalogo.Repositories;

public interface ISkusRepository
{
    public Task<IEnumerable<Skus>> ObterSkusPorProduto(int produtoId);
    public Task<Skus?> ObterSkuPorSku(string sku);
    public Task<Skus> CriarSku(int produtoId, Skus skuData);
    public Task<Skus> AtualizarSku(string sku, Skus skuData);
    public Task<bool> DeletarSku(string sku);
    public Task<IEnumerable<SkusResumo>> PesquisarSkus(string termo);
}
