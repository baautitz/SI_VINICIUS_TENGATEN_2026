using backend.Modules.Acesso.DTOs;
using Backend.Core.Acesso.Entities;

namespace Backend.Core.Acesso.Repositories;

public interface ISessoesRepository
{
    public Task<Sessoes?> ObterSessaoPorToken(string token);
    public Task<Sessoes> CriarSessao(Sessoes sessao);
    public Task<bool> EncerrarSessao(long id);
    public Task<IEnumerable<Sessoes>> ObterSessoesPorUsuario(int usuarioId);
}
