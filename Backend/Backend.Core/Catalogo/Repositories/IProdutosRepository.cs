using Backend.Core.Catalogo.DTOs;
using Backend.Core.Catalogo.Entities;

namespace Backend.Core.Catalogo.Repositories;

public interface IProdutosRepository
{
    public Task<IEnumerable<Produtos>> ObterProdutos();
    public Task<Produtos?> ObterProdutoPorId(int id);
    public Task<Produtos?> ObterProdutoPorSku(string sku);
    public Task<Produtos> CriarProduto(Produtos produto);
    public Task<Produtos> AtualizarProduto(int id, Produtos produto);
    public Task<bool> DeletarProduto(int id);
    public Task<IEnumerable<ProdutosResumo>> ObterProdutosResumo();
    public Task<IEnumerable<ProdutosResumo>> PesquisarProdutos(string termo);
}
