using backend.Modules.Pagamentos.DTOs;
using backend.Modules.Pagamentos.Models;

namespace backend.Modules.Pagamentos.Repositories;

public interface IMetodosPagamentosRepository
{
    public Task<IEnumerable<MetodosPagamentos>> ObterMetodosPagamentos();
    public Task<MetodosPagamentos?> ObterMetodoPagamentoPorId(int id);
    public Task<MetodosPagamentos> CriarMetodoPagamento(MetodosPagamentos metodo);
    public Task<MetodosPagamentos> AtualizarMetodoPagamento(int id, MetodosPagamentos metodo);
    public Task<bool> DeletarMetodoPagamento(int id);
    public Task<IEnumerable<MetodosPagamentosResumo>> ObterMetodosPagamentosResumo();
    public Task<IEnumerable<MetodosPagamentosResumo>> PesquisarMetodosPagamentos(string termo);
}
