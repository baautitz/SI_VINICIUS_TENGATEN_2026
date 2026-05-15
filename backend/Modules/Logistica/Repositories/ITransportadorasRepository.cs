using backend.Modules.Logistica.DTOs;
using backend.Modules.Logistica.Models;

namespace backend.Modules.Logistica.Repositories;

public interface ITransportadorasRepository
{
    public Task<IEnumerable<Transportadoras>> ObterTransportadoras();
    public Task<Transportadoras?> ObterTransportadoraPorId(int id);
    public Task<Transportadoras> CriarTransportadora(Transportadoras transportadora);
    public Task<Transportadoras> AtualizarTransportadora(int id, Transportadoras transportadora);
    public Task<bool> DeletarTransportadora(int id);
    public Task<IEnumerable<TransportadorasResumo>> ObterTransportadorasResumo();
    public Task<IEnumerable<TransportadorasResumo>> PesquisarTransportadoras(string termo);
}
