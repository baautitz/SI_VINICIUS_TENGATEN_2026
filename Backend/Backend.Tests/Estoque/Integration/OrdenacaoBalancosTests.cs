using Backend.Core.Common.Exceptions;
using Backend.Infrastructure.PostgreSQL.Features.Estoque;
using Backend.Tests.Common;

namespace Backend.Tests.Estoque.Integration;

[Trait("Category", "Integration")]
[Collection(PostgresCollection.Nome)]
public class OrdenacaoBalancosTests
{
    private readonly PostgresFixture _db;

    public OrdenacaoBalancosTests(PostgresFixture db)
    {
        _db = db;
        _db.Executar("TRUNCATE balancos RESTART IDENTITY CASCADE;").GetAwaiter().GetResult();
        // ids 1..4 com aberturas fora de ordem; enum de status: ABERTO < FECHADO < CANCELADO.
        _db.Executar(@"INSERT INTO balancos (data_abertura, status) VALUES
            ('2026-01-02', 'ABERTO'), ('2026-01-04', 'FECHADO'), ('2026-01-01', 'ABERTO'), ('2026-01-03', 'CANCELADO');").GetAwaiter().GetResult();
    }

    private async Task<int[]> Ids(string? ordenarPor, string? direcao, int pagina = 1, int tamanho = 20)
    {
        using var s = _db.NovaSessao();
        var r = await new BalancosRepository(s).ObterBalancos(pagina, tamanho, ordenarPor, direcao);
        return r.Itens.Select(b => b.Id).ToArray();
    }

    [Fact]
    public async Task Sem_escolha_abre_pela_mais_recente()
        => Assert.Equal(new[] { 2, 4, 1, 3 }, await Ids(null, null));

    [Fact]
    public async Task Ordena_pelo_campo_e_direcao_escolhidos()
    {
        Assert.Equal(new[] { 3, 1, 4, 2 }, await Ids("dataAbertura", "asc"));
        Assert.Equal(new[] { 4, 2, 3, 1 }, await Ids("status", "desc"));
    }

    [Fact]
    public async Task Paginacao_mantem_a_ordem_escolhida()
    {
        Assert.Equal(new[] { 3, 1 }, await Ids("dataAbertura", "asc", pagina: 1, tamanho: 2));
        Assert.Equal(new[] { 4, 2 }, await Ids("dataAbertura", "asc", pagina: 2, tamanho: 2));
    }

    [Fact]
    public async Task Campo_fora_da_lista_branca_e_recusado()
        => await Assert.ThrowsAsync<DomainException>(() => Ids("usuario_id; DROP TABLE balancos", "asc"));
}
