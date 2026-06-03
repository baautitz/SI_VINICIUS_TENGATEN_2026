using Backend.Core.Common.Results;
using Backend.Core.Features.Catalogo.DTOs;

using Backend.Core.Features.Catalogo.Entities;

namespace Backend.Core.Features.Catalogo.Repositories;

public interface ICategoriasRepository
{
    public Task<ResultadoPaginado<Categorias>> ObterCategorias(int pagina = 1, int tamanhoDaPagina = 20);
    public Task<Categorias?> ObterCategoriaPorId(int id);
    public Task<Categorias> CriarCategoria(Categorias categoria);
    public Task<Categorias> AtualizarCategoria(int id, Categorias categoria);
    public Task<bool> DeletarCategoria(int id);
    public Task<ResultadoPaginado<CategoriasResumo>> ObterCategoriasResumo(int pagina = 1, int tamanhoDaPagina = 20);
    public Task<ResultadoPaginado<CategoriasResumo>> PesquisarCategorias(string termo, int pagina = 1, int tamanhoDaPagina = 20);
    public Task<bool> ExisteCategoria(string categoria, int? ignorarId = null);
}
