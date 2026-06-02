using Backend.Core.Common.Results;
using Backend.Core.Features.Financeiro.DTOs;

using Backend.Core.Features.Financeiro.Entities;

namespace Backend.Core.Features.Financeiro.Repositories; 

public interface IContasReceberRepository {
    public Task<ResultadoPaginado<ContasReceber>> ObterContasReceber(int pagina = 1, int tamanhoDaPagina = 20);
    public Task<ContasReceber?> ObterContaReceberPorId(int id);
    public Task<ContasReceber> CriarContaReceber(ContasReceber conta);
    public Task<ContasReceber> AtualizarContaReceber(int id, ContasReceber conta);
    public Task<bool> DeletarContaReceber(int id);
    public Task<ResultadoPaginado<ContasReceberResumo>> ObterContasReceberResumo(int pagina = 1, int tamanhoDaPagina = 20);
    public Task<ResultadoPaginado<ContasReceberResumo>> PesquisarContasReceber(string termo, int pagina = 1, int tamanhoDaPagina = 20);
}
