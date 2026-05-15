using backend.Modules.Catalogo.DTOs;
using backend.Modules.Catalogo.Models;

namespace backend.Modules.Catalogo.Repositories;

public interface ISkusRepository
{
    public Task<IEnumerable<Skus>> ObterSkusPorProduto(int produtoId);
    public Task<Skus?> ObterSkuPorId(int id);
    public Task<Skus> CriarSku(int produtoId, Skus sku);
    public Task<Skus> AtualizarSku(int id, Skus sku);
    public Task<bool> DeletarSku(int id);
    public Task<IEnumerable<SkusResumo>> PesquisarSkus(string termo);
}
