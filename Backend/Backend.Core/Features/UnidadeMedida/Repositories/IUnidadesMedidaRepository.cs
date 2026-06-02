using Backend.Core.Common.Results;
using Backend.Core.Features.UnidadeMedida.DTOs;

using Backend.Core.Features.UnidadeMedida.Entities;

namespace Backend.Core.Features.UnidadeMedida.Repositories; 

public interface IUnidadesMedidaRepository {
    public Task<ResultadoPaginado<UnidadesMedida>> ObterUnidadesMedida(int pagina = 1, int tamanhoDaPagina = 20);
    public Task<UnidadesMedida?> ObterUnidadeMedidaPorId(int id);
    public Task<UnidadesMedida> CriarUnidadeMedida(UnidadesMedida unidadeMedida);
    public Task<UnidadesMedida> AtualizarUnidadeMedida(int id, UnidadesMedida unidadeMedida);
    public Task<bool> DeletarUnidadeMedida(int id);
    public Task<ResultadoPaginado<UnidadesMedidaResumo>> ObterUnidadesMedidaResumo(int pagina = 1, int tamanhoDaPagina = 20);
    public Task<ResultadoPaginado<UnidadesMedidaResumo>> PesquisarUnidadesMedida(string termo, int pagina = 1, int tamanhoDaPagina = 20);
}
