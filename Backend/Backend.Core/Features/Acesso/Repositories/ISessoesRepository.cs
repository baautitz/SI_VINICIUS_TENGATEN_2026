using Backend.Core.Common;

using Backend.Core.Features.Acesso.Entities;

namespace Backend.Core.Features.Acesso.Repositories;

public interface ISessoesRepository
{
    public Task<Sessoes?> ObterSessaoPorToken(string token);
    public Task<Sessoes> CriarSessao(Sessoes sessao);
    public Task<bool> EncerrarSessao(long id);
    public Task<ResultadoPaginado<Sessoes>> ObterSessoesPorUsuario(int usuarioId, int pagina = 1, int tamanhoDaPagina = 20);
}
