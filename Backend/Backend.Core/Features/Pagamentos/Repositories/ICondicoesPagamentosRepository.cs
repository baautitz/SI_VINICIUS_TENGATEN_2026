using Backend.Core.Common;

using Backend.Core.Features.Pagamentos.DTOs;

using Backend.Core.Features.Pagamentos.Entities;

namespace Backend.Core.Features.Pagamentos.Repositories; 

public interface ICondicoesPagamentosRepository {
    public Task<ResultadoPaginado<CondicoesPagamentos>> ObterCondicoesPagamentos(int pagina = 1, int tamanhoDaPagina = 20);
    public Task<CondicoesPagamentos?> ObterCondicaoPagamentoPorId(int id);
    public Task<CondicoesPagamentos> CriarCondicaoPagamento(CondicoesPagamentos condicao);
    public Task<CondicoesPagamentos> AtualizarCondicaoPagamento(int id, CondicoesPagamentos condicao);
    public Task<bool> DeletarCondicaoPagamento(int id);
    public Task<ResultadoPaginado<CondicoesPagamentosResumo>> ObterCondicoesPagamentosResumo(int pagina = 1, int tamanhoDaPagina = 20);
    public Task<ResultadoPaginado<CondicoesPagamentosResumo>> PesquisarCondicoesPagamentos(string termo, int pagina = 1, int tamanhoDaPagina = 20);
}
