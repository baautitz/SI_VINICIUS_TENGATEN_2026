using Backend.Core.Common.Results;
using Backend.Core.Features.Financeiro.DTOs;
using Backend.Core.Features.Financeiro.Entities;

namespace Backend.Core.Features.Financeiro.Repositories;

public interface IContasReceberRepository
{
    public Task<ContasReceber?> ObterContaReceberPorId(int id);
    // SELECT ... FOR UPDATE: exige transação aberta; serializa recebimentos/estornos concorrentes da mesma conta.
    public Task<ContasReceber?> ObterContaReceberPorIdParaAtualizar(int id);
    public Task<ContasReceber> CriarContaReceber(ContasReceber conta);
    public Task<ContasReceber> AtualizarContaReceber(int id, ContasReceber conta);
    public Task<bool> DeletarContaReceber(int id);
    public Task<ResultadoPaginado<ContasReceber>> PesquisarContasReceber(string termo, int pagina = 1, int tamanhoDaPagina = 20, FiltroContasReceber? filtro = null);
}
