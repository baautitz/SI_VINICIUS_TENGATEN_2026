using Backend.Core.Common.Results;
using Backend.Core.Features.Vendas.DTOs;
using Backend.Core.Features.Vendas.Entities;

namespace Backend.Core.Features.Vendas.Repositories;

// Devolução é imutável: cria, consulta e marca como estornada.
public interface IDevolucoesRepository
{
    public Task<Devolucao?> ObterDevolucaoPorId(int id);
    public Task<IReadOnlyList<Devolucao>> ObterDevolucoesPorVenda(int vendaId);
    public Task<ResultadoPaginado<DevolucaoResumo>> PesquisarDevolucoes(string termo, int pagina = 1, int tamanhoDaPagina = 20, int? vendaId = null);
    public Task<Devolucao> CriarDevolucao(Devolucao devolucao);
    // UPDATE guardado (status ATIVA): false se outro estorno já a marcou.
    public Task<bool> MarcarEstornada(Devolucao devolucao);
}
