using Backend.Core.Common;
using Backend.Core.Features.Localizacao.DTOs;
using Backend.Core.Features.Localizacao.Entities;

namespace Backend.Core.Features.Localizacao.Repositories;

public interface IBairrosRepository
{
    public Task<ResultadoPaginado<Bairros>> ObterBairros(int pagina = 1, int tamanhoDaPagina = 20);
    public Task<Bairros?> ObterBairroPorId(int id);
    public Task<Bairros> CriarBairro(Bairros cidade);
    public Task<Bairros> AtualizarBairro(int id, Bairros cidade);
    public Task<bool> DeletarBairro(int id);
    public Task<ResultadoPaginado<BairroResumoDto>> ObterBairrosResumo(int pagina = 1, int tamanhoDaPagina = 20);
    public Task<ResultadoPaginado<BairroResumoDto>> PesquisarBairros(string termo, int pagina = 1, int tamanhoDaPagina = 20);
}

