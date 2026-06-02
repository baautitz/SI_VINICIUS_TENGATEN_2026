using Backend.Core.Common.Results;
using Backend.Core.Features.Pagamentos.DTOs;

using Backend.Core.Features.Pagamentos.Entities;

namespace Backend.Core.Features.Pagamentos.Repositories; 

public interface IMetodosPagamentosRepository {
    public Task<ResultadoPaginado<MetodosPagamentos>> ObterMetodosPagamentos(int pagina = 1, int tamanhoDaPagina = 20);
    public Task<MetodosPagamentos?> ObterMetodoPagamentoPorId(int id);
    public Task<MetodosPagamentos> CriarMetodoPagamento(MetodosPagamentos metodo);
    public Task<MetodosPagamentos> AtualizarMetodoPagamento(int id, MetodosPagamentos metodo);
    public Task<bool> DeletarMetodoPagamento(int id);
    public Task<ResultadoPaginado<MetodosPagamentosResumo>> ObterMetodosPagamentosResumo(int pagina = 1, int tamanhoDaPagina = 20);
    public Task<ResultadoPaginado<MetodosPagamentosResumo>> PesquisarMetodosPagamentos(string termo, int pagina = 1, int tamanhoDaPagina = 20);
}
