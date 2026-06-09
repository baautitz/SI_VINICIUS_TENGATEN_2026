using Backend.Core.Common.Results;
using Backend.Core.Features.Localizacao.Entities;

namespace Backend.Core.Features.Localizacao.Repositories;

public interface IPaisesRepository {
    public Task<ResultadoPaginado<Paises>> ObterPaises(int pagina = 1, int tamanhoDaPagina = 20);
    public Task<Paises?> ObterPaisPorId(int id);
    public Task<Paises> CriarPais(Paises pais);
    public Task<Paises> AtualizarPais(int id, Paises pais);
    public Task<bool> DeletarPais(int id);
    public Task<ResultadoPaginado<Paises>> PesquisarPaises(string termo, int pagina = 1, int tamanhoDaPagina = 20);
    public Task<bool> ExistePais(string siglaIso, string pais, int? ignorarId = null);
}
