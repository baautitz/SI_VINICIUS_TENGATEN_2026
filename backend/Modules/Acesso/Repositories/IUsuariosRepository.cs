using backend.Modules.Acesso.DTOs;
using backend.Modules.Acesso.Models;

namespace backend.Modules.Acesso.Repositories;

public interface IUsuariosRepository
{
    public Task<IEnumerable<Usuarios>> ObterUsuarios();
    public Task<Usuarios?> ObterUsuarioPorId(int id);
    public Task<Usuarios> CriarUsuario(Usuarios usuario);
    public Task<Usuarios> AtualizarUsuario(int id, Usuarios usuario);
    public Task<bool> DeletarUsuario(int id);
    public Task<IEnumerable<UsuariosResumo>> ObterUsuariosResumo();
    public Task<IEnumerable<UsuariosResumo>> PesquisarUsuarios(string termo);
}
