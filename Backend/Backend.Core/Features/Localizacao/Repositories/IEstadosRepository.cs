using Backend.Core.Common;

using Backend.Core.Common;

using Backend.Core.Features.Localizacao.DTOs;

using Backend.Core.Features.Localizacao.Entities;

namespace Backend.Core.Features.Localizacao.Repositories; 

public interface IEstadosRepository {
    public Task<ResultadoPaginado<Estados>> ObterEstados(int pagina = 1, int tamanhoDaPagina = 20);
    public Task<Estados?> ObterEstadoPorId(int id);
    public Task<Estados> CriarEstado(Estados estado);
    public Task<Estados> AtualizarEstado(int id, Estados estado);
    public Task<bool> DeletarEstado(int id);
    public Task<ResultadoPaginado<EstadosResumo>> ObterEstadosResumo(int pagina = 1, int tamanhoDaPagina = 20);
    public Task<ResultadoPaginado<EstadosResumo>> PesquisarEstados(string termo, int pagina = 1, int tamanhoDaPagina = 20);
}
