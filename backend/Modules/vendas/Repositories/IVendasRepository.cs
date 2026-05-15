using backend.Modules.Vendas.DTOs;
using backend.Modules.Vendas.Models;

namespace backend.Modules.Vendas.Repositories;

public interface IVendasRepository
{
    public Task<IEnumerable<Vendas>> ObterVendas();
    public Task<Vendas?> ObterVendaPorId(int id);
    public Task<Vendas> CriarVenda(Vendas venda);
    public Task<Vendas> AtualizarVenda(int id, Vendas venda);
    public Task<bool> DeletarVenda(int id);
    public Task<IEnumerable<VendasResumo>> ObterVendasResumo();
    public Task<IEnumerable<VendasResumo>> PesquisarVendas(string termo);
}
