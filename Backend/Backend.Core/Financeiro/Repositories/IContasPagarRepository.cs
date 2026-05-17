using Backend.Core.Financeiro.DTOs;
using Backend.Core.Financeiro.Entities;

namespace Backend.Core.Financeiro.Repositories;

public interface IContasPagarRepository
{
    public Task<IEnumerable<ContasPagar>> ObterContasPagar();
    public Task<ContasPagar?> ObterContaPagarPorId(int id);
    public Task<ContasPagar> CriarContaPagar(ContasPagar conta);
    public Task<ContasPagar> AtualizarContaPagar(int id, ContasPagar conta);
    public Task<bool> DeletarContaPagar(int id);
    public Task<IEnumerable<ContasPagarResumo>> ObterContasPagarResumo();
    public Task<IEnumerable<ContasPagarResumo>> PesquisarContasPagar(string termo);
}
