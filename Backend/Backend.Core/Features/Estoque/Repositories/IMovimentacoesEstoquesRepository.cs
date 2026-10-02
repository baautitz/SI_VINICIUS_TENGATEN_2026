using Backend.Core.Common.Results;
using Backend.Core.Features.Estoque.DTOs;
using Backend.Core.Features.Estoque.Entities;
using Backend.Core.Features.Estoque.Entities.Enums;

namespace Backend.Core.Features.Estoque.Repositories;

public interface IMovimentacoesEstoquesRepository
{
    public Task<ResultadoPaginado<MovimentacoesEstoques>> ObterMovimentacoes(int pagina = 1, int tamanhoDaPagina = 20);
    public Task<MovimentacoesEstoques?> ObterMovimentacaoPorId(int id);
    public Task<MovimentacoesEstoques?> ObterMovimentacaoPorOrigem(OrigemMovimentacaoEstoque origemTipo, int origemId);
    public Task<MovimentacoesEstoques> CriarMovimentacao(MovimentacoesEstoques movimentacao);
    public Task<ResultadoPaginado<MovimentacoesEstoques>> PesquisarMovimentacoes(string termo, int pagina = 1, int tamanhoDaPagina = 20);
    public Task<ResultadoPaginado<KardexLinha>> ObterKardex(string? termo, int pagina = 1, int tamanhoDaPagina = 20);
}
