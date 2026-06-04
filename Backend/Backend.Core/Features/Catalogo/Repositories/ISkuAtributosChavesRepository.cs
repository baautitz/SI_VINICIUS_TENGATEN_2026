using Backend.Core.Common.Results;
using Backend.Core.Features.Catalogo.DTOs;
using Backend.Core.Features.Catalogo.Entities;

namespace Backend.Core.Features.Catalogo.Repositories;

public interface ISkuAtributosChavesRepository
{
    public Task<ResultadoPaginado<SkuAtributosChaves>> ObterAtributos(int pagina = 1, int tamanhoDaPagina = 20);
    public Task<SkuAtributosChaves?> ObterAtributoPorId(int id);
    public Task<SkuAtributosChaves> CriarAtributo(SkuAtributosChaves atributo);
    public Task<SkuAtributosChaves> AtualizarAtributo(int id, SkuAtributosChaves atributo);
    public Task<bool> DeletarAtributo(int id);
    public Task<ResultadoPaginado<SkuAtributosChavesResumo>> ObterAtributosResumo(int pagina = 1, int tamanhoDaPagina = 20);
    public Task<ResultadoPaginado<SkuAtributosChavesResumo>> PesquisarAtributos(string termo, int pagina = 1, int tamanhoDaPagina = 20);
    public Task<bool> ExisteChave(string chave, int? ignorarId = null);
    public Task<List<SkuAtributosValores>> ObterValoresPorIds(IEnumerable<int> ids);
}
