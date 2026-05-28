using System.Linq;
using Backend.Core.Common;
using Backend.Core.Features.Acesso.Entities;
using Backend.Core.Features.Acesso.Repositories;
using Backend.Infrastructure.PostgreSQL.Common;
using Dapper;

namespace Backend.Infrastructure.PostgreSQL.Features.Acesso;

public class SessoesRepository : ISessoesRepository
{
    private readonly DbSession _session;

    public SessoesRepository(DbSession session)
    {
        _session = session;
    }

    public async Task<Sessoes?> ObterSessaoPorToken(string token)
    {
        const string sql = @"
            SELECT s.id, s.token, s.data_criacao, s.data_expiracao, s.ativo,
                   u.id AS UsuarioId, u.nome, u.cpf_cnpj, u.email, u.telefone, u.usuario, u.senha, u.ativo
            FROM sessoes s
            JOIN usuarios u ON u.id = s.usuario_id
            WHERE s.token = @Token;";

        var item = await _session.Connection.QuerySingleOrDefaultAsync<SessionUsuarioDto>(
            sql,
            new { Token = token },
            transaction: _session.Transaction
        );

        if (item is null) return null;

        var usuario = BuildUsuario(new UsuarioDto(item.UsuarioId, item.UsuarioNome, item.UsuarioCpfCnpj, item.UsuarioEmail, item.UsuarioTelefone, item.UsuarioUsuario, item.UsuarioSenha, item.UsuarioAtivo));
        var sessao = new SessaoDto(item.Id, item.Token, item.DataCriacao, item.DataExpiracao, item.Ativo);
        return BuildSessao(sessao, usuario);
    }

    public async Task<Sessoes> CriarSessao(Sessoes sessao)
    {
        const string sql = @"
            INSERT INTO sessoes (token, data_criacao, data_expiracao, ativo, usuario_id)
            VALUES (@Token, @DataCriacao, @DataExpiracao, @Ativo, @UsuarioId)
            RETURNING id;";

        var idGerado = await _session.Connection.ExecuteScalarAsync<long>(
            sql,
            new
            {
                sessao.Token,
                sessao.DataCriacao,
                sessao.DataExpiracao,
                sessao.Ativo,
                UsuarioId = sessao.Usuario.Id
            },
            transaction: _session.Transaction
        );

        return new Sessoes(idGerado, sessao.Usuario, sessao.Token, sessao.DataCriacao, sessao.DataExpiracao, sessao.Ativo);
    }

    public async Task<bool> EncerrarSessao(long id)
    {
        const string sql = @"
            UPDATE sessoes
            SET ativo = false,
                data_expiracao = NOW()
            WHERE id = @Id;";

        var linhasAfetadas = await _session.Connection.ExecuteAsync(
            sql,
            new { Id = id },
            transaction: _session.Transaction
        );

        return linhasAfetadas > 0;
    }

    public async Task<ResultadoPaginado<Sessoes>> ObterSessoesPorUsuario(int usuarioId, int pagina = 1, int tamanhoDaPagina = 20)
    {
        var offset = (pagina - 1) * tamanhoDaPagina;

        const string countSql = "SELECT COUNT(*) FROM sessoes WHERE usuario_id = @UsuarioId;";

        const string querySql = @"
            SELECT s.id, s.token, s.data_criacao, s.data_expiracao, s.ativo,
                   u.id AS UsuarioId, u.nome, u.cpf_cnpj, u.email, u.telefone, u.usuario, u.senha, u.ativo
            FROM sessoes s
            JOIN usuarios u ON u.id = s.usuario_id
            WHERE s.usuario_id = @UsuarioId
            ORDER BY s.data_criacao DESC
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        var total = await _session.Connection.ExecuteScalarAsync<int>(
            countSql, new { UsuarioId = usuarioId }, transaction: _session.Transaction);

        var itens = await _session.Connection.QueryAsync<SessionUsuarioDto>(
            querySql,
            new { UsuarioId = usuarioId, TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction
        );

        var sessoes = itens.Select(item =>
        {
            var usuario = BuildUsuario(new UsuarioDto(item.UsuarioId, item.UsuarioNome, item.UsuarioCpfCnpj, item.UsuarioEmail, item.UsuarioTelefone, item.UsuarioUsuario, item.UsuarioSenha, item.UsuarioAtivo));
            var sessao = new SessaoDto(item.Id, item.Token, item.DataCriacao, item.DataExpiracao, item.Ativo);
            return BuildSessao(sessao, usuario);
        });

        return new ResultadoPaginado<Sessoes>(sessoes, total, pagina, tamanhoDaPagina);
    }

    private static Usuarios BuildUsuario(UsuarioDto dto)
    {
        return new Usuarios(dto.Id, dto.Nome, dto.CpfCnpj, dto.Email, dto.Usuario, dto.Senha, dto.Telefone, dto.Ativo);
    }

    private static Sessoes BuildSessao(SessaoDto dto, Usuarios usuario)
    {
        return new Sessoes(dto.Id, usuario, dto.Token, dto.DataCriacao, dto.DataExpiracao, dto.Ativo);
    }

    private sealed record UsuarioDto(int Id, string Nome, string CpfCnpj, string Email, string Telefone, string Usuario, string Senha, bool Ativo);
    private sealed record SessaoDto(long Id, string Token, DateTime DataCriacao, DateTime? DataExpiracao, bool Ativo);
    private sealed record SessionUsuarioDto(
        long Id,
        string Token,
        DateTime DataCriacao,
        DateTime? DataExpiracao,
        bool Ativo,
        int UsuarioId,
        string UsuarioNome,
        string UsuarioCpfCnpj,
        string UsuarioEmail,
        string UsuarioTelefone,
        string UsuarioUsuario,
        string UsuarioSenha,
        bool UsuarioAtivo);
}
