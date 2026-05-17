using Backend.Core.Parceiros.DTOs;
using Backend.Core.Parceiros.Entities;

namespace Backend.Core.Parceiros.Repositories;

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
