using backend.Modules.Localizacao.DTOs;
using backend.Modules.Localizacao.Models;

namespace backend.Modules.Localizacao.Repositories;

public interface ICidadesRepository
{
    public Task<IEnumerable<Cidades>> ObterCidades();
    public Task<Cidades?> ObterCidadePorId(int id);
    public Task<Cidades> CriarCidade(Cidades cidade);
    public Task<Cidades> AtualizarCidade(int id, Cidades cidade);
    public Task<bool> DeletarCidade(int id);
    public Task<IEnumerable<CidadesResumo>> ObterCidadesResumo();
    public Task<IEnumerable<CidadesResumo>> PesquisarCidades(string termo);

}
