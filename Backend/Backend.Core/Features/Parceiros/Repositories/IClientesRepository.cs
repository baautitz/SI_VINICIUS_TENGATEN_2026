using Backend.Core.Common.Results;
using Backend.Core.Features.Parceiros.Entities;

namespace Backend.Core.Features.Parceiros.Repositories;

public interface IClientesRepository
{
    public Task<ResultadoPaginado<Clientes>> ObterClientes(int pagina = 1, int tamanhoDaPagina = 20);
    public Task<Clientes?> ObterClientePorId(int id);
    public Task<Clientes> CriarCliente(Clientes cliente);
    public Task<Clientes> AtualizarCliente(int id, Clientes cliente);
    public Task<bool> DeletarCliente(int id);
    public Task<ResultadoPaginado<Clientes>> PesquisarClientes(string termo, int pagina = 1, int tamanhoDaPagina = 20);
    public Task<bool> ExisteClienteCpfCnpj(string cpfCnpj, int nacionalidadeId, int? ignorarId = null);
}
