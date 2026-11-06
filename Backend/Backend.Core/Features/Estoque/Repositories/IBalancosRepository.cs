using Backend.Core.Common.Results;
using Backend.Core.Features.Estoque.Entities;

namespace Backend.Core.Features.Estoque.Repositories;

public interface IBalancosRepository
{
    public Task<ResultadoPaginado<Balancos>> ObterBalancos(int pagina = 1, int tamanhoDaPagina = 20, string? ordenarPor = null, string? direcao = null);
    public Task<Balancos?> ObterBalancoPorId(int id);
    public Task<Balancos> CriarBalanco(Balancos balanco);
    // Atualiza o cabeçalho e substitui os itens.
    public Task AtualizarBalanco(Balancos balanco);
}
