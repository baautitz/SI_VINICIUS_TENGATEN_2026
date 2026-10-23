using Backend.Core.Common.Results;
using Backend.Core.Features.Financeiro.DTOs;
using Backend.Core.Features.Financeiro.Entities;

namespace Backend.Core.Features.Financeiro.Repositories;

public interface IContasPagarRepository
{
    public Task<ContasPagar?> ObterContaPagarPorId(int id);
    // SELECT ... FOR UPDATE: exige transação aberta; serializa pagamentos/estornos concorrentes da mesma conta.
    public Task<ContasPagar?> ObterContaPagarPorIdParaAtualizar(int id);
    public Task<ContasPagar> CriarContaPagar(ContasPagar conta);
    public Task<ContasPagar> AtualizarContaPagar(int id, ContasPagar conta);
    public Task<bool> DeletarContaPagar(int id);
    public Task<ResultadoPaginado<ContasPagar>> PesquisarContasPagar(string termo, int pagina = 1, int tamanhoDaPagina = 20, FiltroContasPagar? filtro = null);
}
