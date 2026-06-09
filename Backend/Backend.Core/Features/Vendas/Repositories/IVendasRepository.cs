using Backend.Core.Common.Results;
using Backend.Core.Features.Vendas.DTOs;

using Backend.Core.Features.Vendas.Entities;

namespace Backend.Core.Features.Vendas.Repositories;

public interface IVendasRepository
{
    public Task<ResultadoPaginado<Venda>> ObterVendas(int pagina = 1, int tamanhoDaPagina = 20);
    public Task<Venda?> ObterVendaPorId(int id);
    public Task<Venda> CriarVenda(Venda venda);
    public Task<Venda> AtualizarVenda(int id, Venda venda);
    public Task<bool> DeletarVenda(int id);
    public Task<ResultadoPaginado<VendasResumo>> ObterVendasResumo(int pagina = 1, int tamanhoDaPagina = 20);
    public Task<ResultadoPaginado<VendasResumo>> PesquisarVendas(string termo, int pagina = 1, int tamanhoDaPagina = 20);
}
