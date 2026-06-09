using Backend.Core.Common.Results;
using Backend.Core.Features.Localizacao.Entities;

namespace Backend.Core.Features.Localizacao.Repositories;

public interface ICidadesRepository
{
    public Task<ResultadoPaginado<Cidades>> ObterCidades(int pagina = 1, int tamanhoDaPagina = 20);
    public Task<Cidades?> ObterCidadePorId(int id);
    public Task<Cidades> CriarCidade(Cidades cidade);
    public Task<Cidades> AtualizarCidade(int id, Cidades cidade);
    public Task<bool> DeletarCidade(int id);
    public Task<ResultadoPaginado<Cidades>> PesquisarCidades(string termo, int pagina = 1, int tamanhoDaPagina = 20);
    public Task<bool> ExisteCidade(string cidade, int estadoId, int? ignorarId = null);
}
