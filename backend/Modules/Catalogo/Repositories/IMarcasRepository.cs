using backend.Modules.Catalogo.DTOs;
using backend.Modules.Catalogo.Models;

namespace backend.Modules.Catalogo.Repositories;

public interface IMarcasRepository
{
    public Task<IEnumerable<Marcas>> ObterMarcas();
    public Task<Marcas?> ObterMarcaPorId(int id);
    public Task<Marcas> CriarMarca(Marcas marca);
    public Task<Marcas> AtualizarMarca(int id, Marcas marca);
    public Task<bool> DeletarMarca(int id);
    public Task<IEnumerable<MarcasResumo>> ObterMarcasResumo();
    public Task<IEnumerable<MarcasResumo>> PesquisarMarcas(string termo);
}
