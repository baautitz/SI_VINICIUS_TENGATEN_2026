using backend.Modules.Pagamentos.DTOs;
using backend.Modules.Pagamentos.Models;

namespace backend.Modules.Pagamentos.Repositories;

public interface ICondicoesPagamentosRepository
{
    public Task<IEnumerable<CondicoesPagamentos>> ObterCondicoesPagamentos();
    public Task<CondicoesPagamentos?> ObterCondicaoPagamentoPorId(int id);
    public Task<CondicoesPagamentos> CriarCondicaoPagamento(CondicoesPagamentos condicao);
    public Task<CondicoesPagamentos> AtualizarCondicaoPagamento(int id, CondicoesPagamentos condicao);
    public Task<bool> DeletarCondicaoPagamento(int id);
    public Task<IEnumerable<CondicoesPagamentosResumo>> ObterCondicoesPagamentosResumo();
    public Task<IEnumerable<CondicoesPagamentosResumo>> PesquisarCondicoesPagamentos(string termo);
}
