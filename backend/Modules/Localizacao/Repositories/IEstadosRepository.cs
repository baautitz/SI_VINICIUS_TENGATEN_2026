using backend.Modules.Localizacao.DTOs;
using backend.Modules.Localizacao.Models;

namespace backend.Modules.Localizacao.Repositories;

public interface IEstadosRepository
{
    public Task<IEnumerable<Estados>> ObterEstados();
    public Task<Estados?> ObterEstadoPorId(int id);
    public Task<Estados> CriarEstado(Estados estado);
    public Task<Estados> AtualizarEstado(int id, Estados estado);
    public Task<bool> DeletarEstado(int id);
    public Task<IEnumerable<EstadosResumo>> ObterEstadosResumo();
    public Task<IEnumerable<EstadosResumo>> PesquisarEstados(string termo);

}
