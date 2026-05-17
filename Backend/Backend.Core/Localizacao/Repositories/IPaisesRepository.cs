using Backend.Core.Localizacao.DTOs;
using Backend.Core.Localizacao.Entities;

namespace Backend.Core.Localizacao.Repositories;

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
