using backend.Modules.Acesso.DTOs;
using backend.Modules.Acesso.Models;

namespace backend.Modules.Acesso.Repositories;

public interface ISessoesRepository
{
    public Task<Sessoes?> ObterSessaoPorToken(string token);
    public Task<Sessoes> CriarSessao(Sessoes sessao);
    public Task<bool> EncerrarSessao(long id);
    public Task<IEnumerable<Sessoes>> ObterSessoesPorUsuario(int usuarioId);
}
