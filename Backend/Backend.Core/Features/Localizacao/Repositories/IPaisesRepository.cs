using Backend.Core.Common;

using Backend.Core.Common;

using Backend.Core.Features.Localizacao.DTOs;

using Backend.Core.Features.Localizacao.Entities;

namespace Backend.Core.Features.Localizacao.Repositories; 

public interface IPaisesRepository { Task<ResultadoPaginado<Paises>> ObterPaises(int pagina = 1, int tamanhoPagina = 20); Task<Paises?> ObterPaisPorId(int id); Task<Paises> CriarPais(Paises pais); Task<Paises> AtualizarPais(int id, Paises pais); Task<bool> DeletarPais(int id); Task<ResultadoPaginado<PaisesResumo>> ObterPaisesResumo(int pagina = 1, int tamanhoPagina = 20); Task<ResultadoPaginado<PaisesResumo>> PesquisarPaises(string termo, int pagina = 1, int tamanhoPagina = 20);
}
