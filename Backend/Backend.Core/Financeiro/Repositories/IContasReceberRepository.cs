using Backend.Core.Financeiro.DTOs;
using Backend.Core.Financeiro.Entities;

namespace Backend.Core.Financeiro.Repositories;

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
