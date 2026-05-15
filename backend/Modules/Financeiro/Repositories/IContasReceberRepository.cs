using backend.Modules.Financeiro.DTOs;
using backend.Modules.Financeiro.Models;

namespace backend.Modules.Financeiro.Repositories;

public interface IContasReceberRepository
{
    public Task<IEnumerable<ContasReceber>> ObterContasReceber();
    public Task<ContasReceber?> ObterContaReceberPorId(int id);
    public Task<ContasReceber> CriarContaReceber(ContasReceber conta);
    public Task<ContasReceber> AtualizarContaReceber(int id, ContasReceber conta);
    public Task<bool> DeletarContaReceber(int id);
    public Task<IEnumerable<ContasReceberResumo>> ObterContasReceberResumo();
    public Task<IEnumerable<ContasReceberResumo>> PesquisarContasReceber(string termo);
}
