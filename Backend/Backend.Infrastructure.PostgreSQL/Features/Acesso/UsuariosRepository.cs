using System.Linq;
using Backend.Core.Common.Results;
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
            ORDER BY id DESC
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        using var multi = await _session.Connection.QueryMultipleAsync(
            sql,
            new { TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction
        );

        var total = await multi.ReadSingleAsync<int>();
        var itens = (await multi.ReadAsync<UsuarioDto>())
            .Select(dto => BuildUsuario(dto));

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

        var usuarioDto = await multi.ReadSingleOrDefaultAsync<UsuarioDto>();
        if (usuarioDto is null) return null;

        var usuario = BuildUsuario(usuarioDto);
        var sessoesDto = await multi.ReadAsync<SessaoDto>();
        foreach (var sessao in sessoesDto.Select(dto => BuildSessao(dto, usuario)))
        {
            usuario.AdicionarSessao(sessao);
        }

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

        return new Usuarios(idGerado, usuario.Nome, usuario.CpfCnpj, usuario.Email, usuario.Usuario, usuario.Senha, usuario.Telefone, usuario.Ativo);
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

        return new Usuarios(id, usuario.Nome, usuario.CpfCnpj, usuario.Email, usuario.Usuario, usuario.Senha, usuario.Telefone, usuario.Ativo);
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
            ORDER BY id DESC
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
            ORDER BY id DESC
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

    private static Sessoes BuildSessao(SessaoDto dto, Usuarios usuario)
    {
        return new Sessoes(dto.Id, usuario, dto.Token, dto.DataCriacao, dto.DataExpiracao, dto.Ativo);
    }

    private static Usuarios BuildUsuario(UsuarioDto dto, IEnumerable<Sessoes>? sessoes = null)
    {
        var usuario = new Usuarios(dto.Id, dto.Nome, dto.CpfCnpj, dto.Email, dto.Usuario, dto.Senha, dto.Telefone, dto.Ativo);
        if (sessoes is not null)
        {
            foreach (var sessao in sessoes)
            {
                usuario.AdicionarSessao(sessao);
            }
        }

        return usuario;
    }

    private sealed record UsuarioDto(int Id, string Nome, string CpfCnpj, string Email, string Telefone, string Usuario, string Senha, bool Ativo);
    private sealed record SessaoDto(long Id, string Token, DateTime DataCriacao, DateTime? DataExpiracao, bool Ativo);
}
