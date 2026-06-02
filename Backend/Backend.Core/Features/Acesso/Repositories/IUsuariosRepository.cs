using Backend.Core.Common.Results;
using Backend.Core.Features.Acesso.DTOs;

using Backend.Core.Features.Acesso.Entities;

namespace Backend.Core.Features.Acesso.Repositories;

public interface IUsuariosRepository
{
    public Task<ResultadoPaginado<Usuarios>> ObterUsuarios(int pagina = 1, int tamanhoDaPagina = 20);
    public Task<Usuarios?> ObterUsuarioPorId(int id);
    public Task<Usuarios> CriarUsuario(Usuarios usuario);
    public Task<Usuarios> AtualizarUsuario(int id, Usuarios usuario);
    public Task<bool> DeletarUsuario(int id);
    public Task<ResultadoPaginado<UsuariosResumo>> ObterUsuariosResumo(int pagina = 1, int tamanhoDaPagina = 20);
    public Task<ResultadoPaginado<UsuariosResumo>> PesquisarUsuarios(string termo, int pagina = 1, int tamanhoDaPagina = 20);
}
