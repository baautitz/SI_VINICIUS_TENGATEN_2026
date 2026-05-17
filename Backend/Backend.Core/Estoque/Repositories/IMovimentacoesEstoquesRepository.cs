using Backend.Core.Estoque.DTOs;
using Backend.Core.Estoque.Entities;

namespace Backend.Core.Estoque.Repositories;

public interface IMovimentacoesEstoquesRepository
{
    public Task<IEnumerable<MovimentacoesEstoques>> ObterMovimentacoes();
    public Task<MovimentacoesEstoques?> ObterMovimentacaoPorId(int id);
    public Task<MovimentacoesEstoques> CriarMovimentacao(MovimentacoesEstoques movimentacao);
    public Task<MovimentacoesEstoques> AtualizarMovimentacao(int id, MovimentacoesEstoques movimentacao);
    public Task<bool> DeletarMovimentacao(int id);
    public Task<IEnumerable<MovimentacoesEstoquesResumo>> ObterMovimentacoesResumo();
    public Task<IEnumerable<MovimentacoesEstoquesResumo>> PesquisarMovimentacoes(string termo);
}
