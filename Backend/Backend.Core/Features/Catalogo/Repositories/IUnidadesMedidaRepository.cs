using Backend.Core.Common.Results;
using Backend.Core.Features.Catalogo.DTOs;
using Backend.Core.Features.Catalogo.Entities;

namespace Backend.Core.Features.Catalogo.Repositories; 

public interface IUnidadesMedidaRepository {
    public Task<ResultadoPaginado<UnidadesMedida>> ObterUnidadesMedida(int pagina = 1, int tamanhoDaPagina = 20);
    public Task<UnidadesMedida?> ObterUnidadeMedidaPorId(int id);
    public Task<UnidadesMedida> CriarUnidadeMedida(UnidadesMedida unidadeMedida);
    public Task<UnidadesMedida> AtualizarUnidadeMedida(int id, UnidadesMedida unidadeMedida);
    public Task<bool> DeletarUnidadeMedida(int id);
    public Task<ResultadoPaginado<UnidadesMedidaResumo>> ObterUnidadesMedidaResumo(int pagina = 1, int tamanhoDaPagina = 20);
    public Task<ResultadoPaginado<UnidadesMedidaResumo>> PesquisarUnidadesMedida(string termo, int pagina = 1, int tamanhoDaPagina = 20);
    public Task<bool> ExisteSigla(string sigla, int? ignorarId = null);
}
