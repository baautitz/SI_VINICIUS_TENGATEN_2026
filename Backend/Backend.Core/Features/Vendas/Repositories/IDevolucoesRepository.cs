using Backend.Core.Features.Vendas.Entities;

namespace Backend.Core.Features.Vendas.Repositories;

// Devolução é imutável: só cria e consulta.
public interface IDevolucoesRepository
{
    public Task<Devolucao?> ObterDevolucaoPorId(int id);
    public Task<IReadOnlyList<Devolucao>> ObterDevolucoesPorVenda(int vendaId);
    public Task<Devolucao> CriarDevolucao(Devolucao devolucao);
}
