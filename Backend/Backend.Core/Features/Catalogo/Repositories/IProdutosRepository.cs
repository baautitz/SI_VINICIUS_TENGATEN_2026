using Backend.Core.Common.Results;
using Backend.Core.Features.Catalogo.Entities;

namespace Backend.Core.Features.Catalogo.Repositories;

public interface IProdutosRepository
{
    public Task<ResultadoPaginado<Produtos>> ObterProdutos(int pagina = 1, int tamanhoDaPagina = 20, int? categoriaId = null, int? marcaId = null, int? unidadeMedidaId = null);
    public Task<Produtos?> ObterProdutoPorId(int id);
    public Task<Produtos?> ObterProdutoPorSku(string sku);
    public Task<Produtos> CriarProduto(Produtos produto);
    public Task<Produtos> AtualizarProduto(int id, Produtos produto);
    public Task<bool> DeletarProduto(int id);
    public Task<ResultadoPaginado<Produtos>> PesquisarProdutos(string termo, int pagina = 1, int tamanhoDaPagina = 20, int? categoriaId = null, int? marcaId = null, int? unidadeMedidaId = null);
    public Task<bool> ExisteProduto(string produto, int? ignorarId = null);
}
