using backend.Modules.Localizacao.DTOs;
using backend.Modules.Localizacao.Models;

namespace backend.Modules.Localizacao.Repositories;

public interface IBairrosRepository
{
    public Task<IEnumerable<Bairros>> ObterBairros();
    public Task<Bairros?> ObterBairroPorId(int id);
    public Task<Bairros> CriarBairro(Bairros cidade);
    public Task<Bairros> AtualizarBairro(int id, Bairros cidade);
    public Task<bool> DeletarBairro(int id);
    public Task<IEnumerable<BairrosResumo>> ObterBairrosResumo();
    public Task<IEnumerable<BairrosResumo>> PesquisarBairros(string termo);

}
