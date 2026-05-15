using backend.Modules.Catalogo.DTOs;
using backend.Modules.Catalogo.Models;

namespace backend.Modules.Catalogo.Repositories;

public interface ICategoriasRepository
{
    public Task<IEnumerable<Categorias>> ObterCategorias();
    public Task<Categorias?> ObterCategoriaPorId(int id);
    public Task<Categorias> CriarCategoria(Categorias categoria);
    public Task<Categorias> AtualizarCategoria(int id, Categorias categoria);
    public Task<bool> DeletarCategoria(int id);
    public Task<IEnumerable<CategoriasResumo>> ObterCategoriasResumo();
    public Task<IEnumerable<CategoriasResumo>> PesquisarCategorias(string termo);
}
