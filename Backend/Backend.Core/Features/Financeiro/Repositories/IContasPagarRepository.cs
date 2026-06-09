using Backend.Core.Common.Results;
using Backend.Core.Features.Financeiro.DTOs;

using Backend.Core.Features.Financeiro.Entities;

namespace Backend.Core.Features.Financeiro.Repositories;

public interface IContasPagarRepository
{
    public Task<ResultadoPaginado<ContasPagar>> ObterContasPagar(int pagina = 1, int tamanhoDaPagina = 20);
    public Task<ContasPagar?> ObterContaPagarPorId(int id);
    public Task<ContasPagar> CriarContaPagar(ContasPagar conta);
    public Task<ContasPagar> AtualizarContaPagar(int id, ContasPagar conta);
    public Task<bool> DeletarContaPagar(int id);
    public Task<ResultadoPaginado<ContasPagarResumo>> ObterContasPagarResumo(int pagina = 1, int tamanhoDaPagina = 20);
    public Task<ResultadoPaginado<ContasPagarResumo>> PesquisarContasPagar(string termo, int pagina = 1, int tamanhoDaPagina = 20);
}
