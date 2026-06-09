using Backend.Core.Common.ValueObjects;
using Backend.Core.Common.Results;

using Backend.Core.Features.Logistica.Entities;

namespace Backend.Core.Features.Logistica.Repositories;

public interface ITransportadorasRepository
{
    public Task<ResultadoPaginado<Transportadoras>> ObterTransportadoras(int pagina = 1, int tamanhoDaPagina = 20);
    public Task<Transportadoras?> ObterTransportadoraPorId(int id);
    public Task<Transportadoras> CriarTransportadora(Transportadoras transportadora);
    public Task<Transportadoras> AtualizarTransportadora(int id, Transportadoras transportadora);
    public Task<bool> DeletarTransportadora(int id);
    public Task<ResultadoPaginado<Transportadoras>> PesquisarTransportadoras(string termo, int pagina = 1, int tamanhoDaPagina = 20);
    public Task<bool> ExisteTransportadoraCpfCnpj(string cpfCnpj, int nacionalidadeId, int? ignorarId = null);
}
