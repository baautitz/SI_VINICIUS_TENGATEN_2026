using Backend.Core.Common.Results;
using Backend.Core.Features.Catalogo.DTOs;

using Backend.Core.Features.Catalogo.Entities;

namespace Backend.Core.Features.Catalogo.Repositories;

public interface IProdutosRepository
{
  public Task<ResultadoPaginado<Produtos>> ObterProdutos(int pagina = 1, int tamanhoDaPagina = 20);
  public Task<Produtos?> ObterProdutoPorId(int id);
  public Task<Produtos?> ObterProdutoPorSku(string sku);
  public Task<Produtos> CriarProduto(Produtos produto);
  public Task<Produtos> AtualizarProduto(int id, Produtos produto);
  public Task<bool> DeletarProduto(int id);
  public Task<ResultadoPaginado<ProdutosResumo>> ObterProdutosResumo(int pagina = 1, int tamanhoDaPagina = 20);
  public Task<ResultadoPaginado<ProdutosResumo>> PesquisarProdutos(string termo, int pagina = 1, int tamanhoDaPagina = 20);
  public Task<bool> ExisteProduto(string produto, int? ignorarId = null);
}
