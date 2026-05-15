using backend.Modules.Parceiros.DTOs;
using backend.Modules.Parceiros.Models;

namespace backend.Modules.Parceiros.Repositories;

public interface IClientesRepository
{
    public Task<IEnumerable<Clientes>> ObterClientes();
    public Task<Clientes?> ObterClientePorId(int id);
    public Task<Clientes> CriarCliente(Clientes cliente);
    public Task<Clientes> AtualizarCliente(int id, Clientes cliente);
    public Task<bool> DeletarCliente(int id);
    public Task<IEnumerable<ClientesResumo>> ObterClientesResumo();
    public Task<IEnumerable<ClientesResumo>> PesquisarClientes(string termo);
}
