using backend.Modules.Catalogo.DTOs;
using backend.Modules.Catalogo.Models;

namespace backend.Modules.Catalogo.Repositories;

public interface IProdutosRepository
{
    public Task<IEnumerable<Produtos>> ObterProdutos();
    public Task<Produtos?> ObterProdutoPorId(int id);
    public Task<Produtos> CriarProduto(Produtos produto);
    public Task<Produtos> AtualizarProduto(int id, Produtos produto);
    public Task<bool> DeletarProduto(int id);
    public Task<IEnumerable<ProdutosResumo>> ObterProdutosResumo();
    public Task<IEnumerable<ProdutosResumo>> PesquisarProdutos(string termo);
}
