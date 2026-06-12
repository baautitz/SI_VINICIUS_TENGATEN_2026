using Backend.Core.Common.Results;
using Backend.Core.Features.Pagamentos.Entities;

namespace Backend.Core.Features.Pagamentos.Repositories;

public interface IMetodosPagamentosRepository
{
    public Task<ResultadoPaginado<MetodosPagamentos>> ObterMetodosPagamentos(int pagina = 1, int tamanhoDaPagina = 20);
    public Task<MetodosPagamentos?> ObterMetodoPagamentoPorCodigo(string codigo);
    public Task<MetodosPagamentos> CriarMetodoPagamento(MetodosPagamentos metodo);
    public Task<MetodosPagamentos> AtualizarMetodoPagamento(string codigo, MetodosPagamentos metodo);
    public Task<bool> DeletarMetodoPagamento(string codigo);
    public Task<ResultadoPaginado<MetodosPagamentos>> PesquisarMetodosPagamentos(string termo, int pagina = 1, int tamanhoDaPagina = 20);
    public Task<bool> ExisteCodigo(string codigo, string? ignorarCodigo = null);
    public Task<string?> ObterUltimoCodigo();
}
