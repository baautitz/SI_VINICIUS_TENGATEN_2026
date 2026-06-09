using Backend.Core.Common.Results;
using Backend.Core.Features.Localizacao.Entities;

namespace Backend.Core.Features.Localizacao.Repositories;

public interface IBairrosRepository
{
    public Task<ResultadoPaginado<Bairros>> ObterBairros(int pagina = 1, int tamanhoDaPagina = 20);
    public Task<Bairros?> ObterBairroPorId(int id);
    public Task<Bairros> CriarBairro(Bairros bairro);
    public Task<Bairros> AtualizarBairro(int id, Bairros bairro);
    public Task<bool> DeletarBairro(int id);
    public Task<ResultadoPaginado<Bairros>> PesquisarBairros(string termo, int pagina = 1, int tamanhoDaPagina = 20);
    public Task<bool> ExisteBairro(string bairro, int cidadeId, int? ignorarId = null);
}
