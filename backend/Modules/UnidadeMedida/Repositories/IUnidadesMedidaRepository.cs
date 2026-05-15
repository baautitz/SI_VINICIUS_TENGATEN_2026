using backend.Modules.UnidadeMedida.DTOs;
using backend.Modules.UnidadeMedida.Models;

namespace backend.Modules.UnidadeMedida.Repositories;

public interface IUnidadesMedidaRepository
{
    public Task<IEnumerable<UnidadesMedida>> ObterUnidadesMedida();
    public Task<UnidadesMedida?> ObterUnidadeMedidaPorId(int id);
    public Task<UnidadesMedida> CriarUnidadeMedida(UnidadesMedida unidadeMedida);
    public Task<UnidadesMedida> AtualizarUnidadeMedida(int id, UnidadesMedida unidadeMedida);
    public Task<bool> DeletarUnidadeMedida(int id);
    public Task<IEnumerable<UnidadesMedidaResumo>> ObterUnidadesMedidaResumo();
    public Task<IEnumerable<UnidadesMedidaResumo>> PesquisarUnidadesMedida(string termo);
}
