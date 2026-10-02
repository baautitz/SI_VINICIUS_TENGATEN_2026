using Backend.Core.Common.Results;
using Backend.Core.Features.Estoque.Entities;

namespace Backend.Core.Features.Estoque.Repositories;

public interface IBalancosRepository
{
    public Task<ResultadoPaginado<Balancos>> ObterBalancos(int pagina = 1, int tamanhoDaPagina = 20);
    public Task<Balancos?> ObterBalancoPorId(int id);
    public Task<Balancos> CriarBalanco(Balancos balanco, IEnumerable<string>? skus);
    public Task AtualizarBalanco(Balancos balanco);
}
