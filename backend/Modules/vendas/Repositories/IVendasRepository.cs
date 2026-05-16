using backend.Modules.Vendas.DTOs;
using backend.Modules.Vendas.Models;

namespace backend.Modules.Vendas.Repositories;

public interface IVendasRepository
{
    public Task<IEnumerable<Venda>> ObterVendas();
    public Task<Venda?> ObterVendaPorId(int id);
    public Task<Venda> CriarVenda(Venda venda);
    public Task<Venda> AtualizarVenda(int id, Venda venda);
    public Task<bool> DeletarVenda(int id);
    public Task<IEnumerable<VendasResumo>> ObterVendasResumo();
    public Task<IEnumerable<VendasResumo>> PesquisarVendas(string termo);
}
