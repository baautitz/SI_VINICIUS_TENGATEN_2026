using Backend.Core.Common;

using Backend.Core.Features.Estoque.DTOs;

using Backend.Core.Features.Estoque.Entities;

namespace Backend.Core.Features.Estoque.Repositories; 

public interface IMovimentacoesEstoquesRepository {
    public Task<ResultadoPaginado<MovimentacoesEstoques>> ObterMovimentacoes(int pagina = 1, int tamanhoDaPagina = 20);
    public Task<MovimentacoesEstoques?> ObterMovimentacaoPorId(int id);
    public Task<MovimentacoesEstoques> CriarMovimentacao(MovimentacoesEstoques movimentacao);
    public Task<MovimentacoesEstoques> AtualizarMovimentacao(int id, MovimentacoesEstoques movimentacao);
    public Task<bool> DeletarMovimentacao(int id);
    public Task<ResultadoPaginado<MovimentacoesEstoquesResumo>> ObterMovimentacoesResumo(int pagina = 1, int tamanhoDaPagina = 20);
    public Task<ResultadoPaginado<MovimentacoesEstoquesResumo>> PesquisarMovimentacoes(string termo, int pagina = 1, int tamanhoDaPagina = 20);
}
