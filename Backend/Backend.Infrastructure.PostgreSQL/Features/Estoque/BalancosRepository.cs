using System.Linq;
using Backend.Core.Common.Results;
using Backend.Core.Features.Acesso.Entities;
using Backend.Core.Features.Estoque.Entities;
using Backend.Core.Features.Estoque.Entities.Enums;
using Backend.Core.Features.Estoque.Repositories;
using Backend.Infrastructure.PostgreSQL.Common;
using Dapper;
using Npgsql;

namespace Backend.Infrastructure.PostgreSQL.Features.Estoque;

public class BalancosRepository : IBalancosRepository
{
    private readonly DbSession _session;

    public BalancosRepository(DbSession session)
    {
        _session = session;
    }

    private const string SelecaoBalanco = @"
        SELECT b.id, b.data_abertura, b.data_fechamento, b.status, b.observacao,
               u.id AS UsuarioId, u.nome AS UsuarioNome, u.cpf_cnpj AS UsuarioCpfCnpj, u.email AS UsuarioEmail,
               u.telefone AS UsuarioTelefone, u.usuario AS UsuarioUsuario, u.senha AS UsuarioSenha, u.ativo AS UsuarioAtivo
        FROM balancos b
        LEFT JOIN usuarios u ON u.id = b.usuario_id";

    public async Task<ResultadoPaginado<Balancos>> ObterBalancos(int pagina = 1, int tamanhoDaPagina = 20)
    {
        var total = await _session.Connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM balancos;", transaction: _session.Transaction);
        var rows = (await _session.Connection.QueryAsync<BalancoDbRow>(
            $"{SelecaoBalanco} ORDER BY b.data_abertura DESC, b.id DESC LIMIT @Tamanho OFFSET @Offset;",
            new { Tamanho = tamanhoDaPagina, Offset = (pagina - 1) * tamanhoDaPagina },
            transaction: _session.Transaction)).ToList();

        return new ResultadoPaginado<Balancos>(await Montar(rows), total, pagina, tamanhoDaPagina);
    }

    public async Task<Balancos?> ObterBalancoPorId(int id)
    {
        var rows = (await _session.Connection.QueryAsync<BalancoDbRow>(
            $"{SelecaoBalanco} WHERE b.id = @Id;", new { Id = id }, transaction: _session.Transaction)).ToList();
        return (await Montar(rows)).FirstOrDefault();
    }

    public async Task<Balancos> CriarBalanco(Balancos balanco)
    {
        try
        {
            var id = await _session.Connection.ExecuteScalarAsync<int>(
                @"INSERT INTO balancos (data_abertura, status, usuario_id, observacao)
                  VALUES (@DataAbertura, @Status::status_balanco_enum, @UsuarioId, @Observacao) RETURNING id;",
                new { balanco.DataAbertura, Status = balanco.Status.ToString(), UsuarioId = balanco.Usuario?.Id, balanco.Observacao },
                transaction: _session.Transaction);

            await InserirItens(id, balanco.Itens);
            return (await ObterBalancoPorId(id))!;
        }
        catch (PostgresException ex)
        {
            throw DbExceptionTranslator.Translate(ex);
        }
    }

    public async Task AtualizarBalanco(Balancos balanco)
    {
        try
        {
            await _session.Connection.ExecuteAsync(
                "UPDATE balancos SET status = @Status::status_balanco_enum, data_fechamento = @DataFechamento, observacao = @Observacao WHERE id = @Id;",
                new { balanco.Id, Status = balanco.Status.ToString(), balanco.DataFechamento, balanco.Observacao },
                transaction: _session.Transaction);

            await _session.Connection.ExecuteAsync(
                "DELETE FROM balancos_itens WHERE balanco_id = @Id;", new { balanco.Id }, transaction: _session.Transaction);
            await InserirItens(balanco.Id, balanco.Itens);
        }
        catch (PostgresException ex)
        {
            throw DbExceptionTranslator.Translate(ex);
        }
    }

    private Task InserirItens(int balancoId, IEnumerable<BalancosItens> itens)
        => _session.Connection.ExecuteAsync(
            @"INSERT INTO balancos_itens (balanco_id, sku, quantidade_sistema, quantidade_contada)
              VALUES (@BalancoId, @Sku, @QuantidadeSistema, @QuantidadeContada);",
            itens.Select(i => new { BalancoId = balancoId, i.Sku, i.QuantidadeSistema, i.QuantidadeContada }),
            transaction: _session.Transaction);

    private async Task<List<Balancos>> Montar(List<BalancoDbRow> rows)
    {
        if (rows.Count == 0) return new List<Balancos>();

        const string itensSql = @"
            SELECT bi.id, bi.balanco_id AS BalancoId, bi.sku, bi.quantidade_sistema AS QuantidadeSistema,
                   bi.quantidade_contada AS QuantidadeContada, u.sigla AS UnidadeMedidaSigla,
                   p.produto || COALESCE((SELECT ' - ' || string_agg(sav.valor, ' / ' ORDER BY sav.id)
                                          FROM skus_atributos_valores_relacionamento savr
                                          JOIN sku_atributos_valores sav ON sav.id = savr.valor_id
                                          WHERE savr.sku = s.sku), '') AS ProdutoNome
            FROM balancos_itens bi
            JOIN skus s ON s.sku = bi.sku
            JOIN produtos p ON p.id = s.produto_id
            JOIN unidades_medida u ON u.id = p.unidade_medida_id
            WHERE bi.balanco_id = ANY(@Ids)
            ORDER BY ProdutoNome, bi.sku;";

        var itens = (await _session.Connection.QueryAsync<ItemDbRow>(
                itensSql, new { Ids = rows.Select(r => r.Id).ToArray() }, transaction: _session.Transaction))
            .GroupBy(i => i.BalancoId).ToDictionary(g => g.Key, g => g.ToList());

        return rows.Select(row =>
        {
            var usuario = row.UsuarioId.HasValue
                ? new Usuarios(row.UsuarioId.Value, row.UsuarioNome ?? string.Empty, row.UsuarioCpfCnpj ?? string.Empty, row.UsuarioEmail ?? string.Empty, row.UsuarioUsuario ?? string.Empty, row.UsuarioSenha ?? string.Empty, row.UsuarioTelefone ?? string.Empty, row.UsuarioAtivo ?? false)
                : null;
            var balanco = new Balancos(row.Id, row.DataAbertura, row.DataFechamento, row.Status, row.Observacao, usuario);
            foreach (var i in itens.GetValueOrDefault(row.Id) ?? new List<ItemDbRow>())
                balanco.AdicionarItem(new BalancosItens(i.Id, i.Sku, i.ProdutoNome, i.UnidadeMedidaSigla, i.QuantidadeSistema, i.QuantidadeContada));
            return balanco;
        }).ToList();
    }

    private sealed record BalancoDbRow(int Id, DateTime DataAbertura, DateTime? DataFechamento, StatusBalanco Status, string? Observacao,
        int? UsuarioId, string? UsuarioNome, string? UsuarioCpfCnpj, string? UsuarioEmail, string? UsuarioTelefone,
        string? UsuarioUsuario, string? UsuarioSenha, bool? UsuarioAtivo);

    private sealed record ItemDbRow(int Id, int BalancoId, string Sku, decimal QuantidadeSistema, decimal? QuantidadeContada, string UnidadeMedidaSigla, string ProdutoNome);
}
