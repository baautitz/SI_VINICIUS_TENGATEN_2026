using Backend.Core.Common;
using Backend.Core.Features.Acesso.DTOs;
using Backend.Core.Features.Acesso.Entities;
using Backend.Core.Features.Acesso.Repositories;
using Backend.Infrastructure.PostgreSQL.Common;
using Dapper;

namespace Backend.Infrastructure.PostgreSQL.Features.Acesso;

public class UsuariosRepository : IUsuariosRepository
{
    private readonly DbSession _session;

    public UsuariosRepository(DbSession session)
    {
        _session = session;
    }

    public async Task<ResultadoPaginado<Usuarios>> ObterUsuarios(int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sql = @"
            SELECT COUNT(*) FROM usuarios;

            SELECT id, nome, cpf_cnpj, email, telefone, usuario, senha, ativo
            FROM usuarios
            ORDER BY nome
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql,
            new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction
        );

        var total = await multi.ReadSingleAsync<int>();
        var itens = (await multi.ReadAsync<Usuarios>())
            .Select(u => { u.Sessoes = []; return u; });

        return new ResultadoPaginado<Usuarios>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<Usuarios?> ObterUsuarioPorId(int id)
    {
        const string sql = @"
            SELECT u.id, u.nome, u.cpf_cnpj, u.email, u.telefone, u.usuario, u.senha, u.ativo
            FROM usuarios u
            WHERE u.id = @Id;

            SELECT id, token, data_criacao, data_expiracao, ativo
            FROM sessoes
            WHERE usuario_id = @Id;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql,
            new { Id = id },
            transaction: _session.Transaction
        );

        var usuario = await multi.ReadSingleOrDefaultAsync<Usuarios>();
        if (usuario is null) return null;

        var sessoes = await multi.ReadAsync<Sessoes>();
        usuario.Sessoes = sessoes;

        return usuario;
    }

    public async Task<Usuarios> CriarUsuario(Usuarios usuario)
    {
        const string sql = @"
            INSERT INTO usuarios (nome, cpf_cnpj, email, telefone, usuario, senha, ativo)
            VALUES (@Nome, @CpfCnpj, @Email, @Telefone, @Usuario, @Senha, @Ativo)
            RETURNING id;";

        var idGerado = await _session.Connection.ExecuteScalarAsync<int>(
            sql,
            usuario,
            transaction: _session.Transaction
        );

        usuario.Id = idGerado;
        return usuario;
    }

    public async Task<Usuarios> AtualizarUsuario(int id, Usuarios usuario)
    {
        const string sql = @"
            UPDATE usuarios
            SET nome = @Nome,
                cpf_cnpj = @CpfCnpj,
                email = @Email,
                telefone = @Telefone,
                usuario = @Usuario,
                senha = @Senha,
                ativo = @Ativo
            WHERE id = @Id;";

        var parametros = new
        {
            Id = id,
            usuario.Nome,
            usuario.CpfCnpj,
            usuario.Email,
            usuario.Telefone,
            usuario.Usuario,
            usuario.Senha,
            usuario.Ativo
        };

        await _session.Connection.ExecuteAsync(sql, parametros, transaction: _session.Transaction);

        usuario.Id = id;
        return usuario;
    }

    public async Task<bool> DeletarUsuario(int id)
    {
        const string sql = "DELETE FROM usuarios WHERE id = @Id;";

        var linhasAfetadas = await _session.Connection.ExecuteAsync(
            sql,
            new { Id = id },
            transaction: _session.Transaction
        );

        return linhasAfetadas > 0;
    }

    public async Task<ResultadoPaginado<UsuariosResumo>> ObterUsuariosResumo(int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sql = @"
            SELECT COUNT(*) FROM usuarios;

            SELECT id, nome, cpf_cnpj, email, usuario
            FROM usuarios
            ORDER BY nome
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql,
            new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction
        );

        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<UsuariosResumo>();

        return new ResultadoPaginado<UsuariosResumo>(itens, total, pagina, tamanhoDaPagina);
    }

    public async Task<ResultadoPaginado<UsuariosResumo>> PesquisarUsuarios(string termo, int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string sql = @"
            SELECT COUNT(*)
            FROM usuarios
            WHERE nome ILIKE @Termo OR email ILIKE @Termo OR usuario ILIKE @Termo;

            SELECT id, nome, cpf_cnpj, email, usuario
            FROM usuarios
            WHERE nome ILIKE @Termo OR email ILIKE @Termo OR usuario ILIKE @Termo
            ORDER BY nome
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql,
            new { Termo = $"%{termo}%", TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction
        );

        var total = await multi.ReadSingleAsync<int>();
        var itens = await multi.ReadAsync<UsuariosResumo>();

        return new ResultadoPaginado<UsuariosResumo>(itens, total, pagina, tamanhoDaPagina);
    }
}
