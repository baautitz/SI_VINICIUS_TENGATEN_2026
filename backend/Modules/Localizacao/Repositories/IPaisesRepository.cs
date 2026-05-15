using backend.Modules.Localizacao.DTOs;
using backend.Modules.Localizacao.Models;

namespace backend.Modules.Localizacao.Repositories;

public interface IPaisesRepository
{
    public Task<IEnumerable<Paises>> ObterPaises();
    public Task<Paises?> ObterPaisPorId(int id);
    public Task<Paises> CriarPais(Paises pais);
    public Task<Paises> AtualizarPais(int id, Paises pais);
    public Task<bool> DeletarPais(int id);
    public Task<IEnumerable<PaisesResumo>> ObterPaisesResumo();
    public Task<IEnumerable<PaisesResumo>> PesquisarPaises(string termo);
}
