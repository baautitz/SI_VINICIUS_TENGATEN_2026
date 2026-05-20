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
            SELECT s.id AS Id, s.token, s.data_criacao, s.data_expiracao, s.ativo,
                   u.id AS UsuarioId, u.nome, u.cpf_cnpj, u.email, u.telefone, u.usuario, u.senha, u.ativo
            FROM sessoes s
            JOIN usuarios u ON u.id = s.usuario_id
            WHERE s.token = @Token;";

        var result = await _session.Connection.QueryAsync<Sessoes, Usuarios, Sessoes>(
            sql,
            (sessao, usuario) =>
            {
                usuario.Sessoes = [];
                sessao.Usuario = usuario;
                return sessao;
            },
            new { Token = token },
            transaction: _session.Transaction,
            splitOn: "UsuarioId"
        );

        return result.SingleOrDefault();
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

        sessao.Id = idGerado;
        return sessao;
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
            SELECT s.id AS Id, s.token, s.data_criacao, s.data_expiracao, s.ativo,
                   u.id AS UsuarioId, u.nome, u.cpf_cnpj, u.email, u.telefone, u.usuario, u.senha, u.ativo
            FROM sessoes s
            JOIN usuarios u ON u.id = s.usuario_id
            WHERE s.usuario_id = @UsuarioId
            ORDER BY s.data_criacao DESC
            LIMIT @TamanhoDaPagina OFFSET @Offset;";

        var total = await _session.Connection.ExecuteScalarAsync<int>(
            countSql, new { UsuarioId = usuarioId }, transaction: _session.Transaction);

        var itens = await _session.Connection.QueryAsync<Sessoes, Usuarios, Sessoes>(
            querySql,
            (sessao, usuario) =>
            {
                usuario.Sessoes = [];
                sessao.Usuario = usuario;
                return sessao;
            },
            new { UsuarioId = usuarioId, TamanhoDaPagina = tamanhoDaPagina, Offset = offset },
            transaction: _session.Transaction,
            splitOn: "UsuarioId"
        );

        return new ResultadoPaginado<Sessoes>(itens, total, pagina, tamanhoDaPagina);
    }
}
