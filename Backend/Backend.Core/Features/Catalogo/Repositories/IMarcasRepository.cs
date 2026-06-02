using Backend.Core.Common.Results;
using Backend.Core.Features.Catalogo.DTOs;

using Backend.Core.Features.Catalogo.Entities;

namespace Backend.Core.Features.Catalogo.Repositories;

public interface IMarcasRepository
{
    public Task<ResultadoPaginado<Marcas>> ObterMarcas(int pagina = 1, int tamanhoDaPagina = 20);
    public Task<Marcas?> ObterMarcaPorId(int id);
    public Task<Marcas> CriarMarca(Marcas marca);
    public Task<Marcas> AtualizarMarca(int id, Marcas marca);
    public Task<bool> DeletarMarca(int id);
    public Task<ResultadoPaginado<MarcasResumo>> ObterMarcasResumo(int pagina = 1, int tamanhoDaPagina = 20);
    public Task<ResultadoPaginado<MarcasResumo>> PesquisarMarcas(string termo, int pagina = 1, int tamanhoDaPagina = 20);
}
