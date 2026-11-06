using Backend.Infrastructure.PostgreSQL.Features.NFe;

namespace Backend.Tests.NFe.Integration;

using Backend.Tests.Common;

[Trait("Category", "Integration")]
[Collection(PostgresCollection.Nome)]
public class OrdenacaoNfesTests
{
    private readonly PostgresFixture _db;

    public OrdenacaoNfesTests(PostgresFixture db)
    {
        _db = db;
        _db.Limpar().GetAwaiter().GetResult();
        _db.Executar("TRUNCATE nfes, emitentes, clientes RESTART IDENTITY CASCADE;").GetAwaiter().GetResult();
        _db.Executar("INSERT INTO emitentes (tipo_pessoa, nome_razaosocial, cpf_cnpj, nacionalidade_id) VALUES ('JURIDICA', 'Emitente', '45723174000110', (SELECT id FROM paises LIMIT 1));").GetAwaiter().GetResult();
        _db.Executar("INSERT INTO clientes (tipo_pessoa, nome_razaosocial, cpf_cnpj, nacionalidade_id) VALUES ('FISICA', 'Maria', '52998224725', (SELECT id FROM paises LIMIT 1));").GetAwaiter().GetResult();
    }

    private Task Nfe(int numero) =>
        _db.Executar(@"INSERT INTO nfes (numero, serie, data_emissao, emitente_id, cliente_id, emitente_nome_razaosocial, emitente_cpf_cnpj,
                cliente_nome_razaosocial, cliente_cpf_cnpj, tipo_operacao)
            VALUES (@numero, 1, '2026-01-01T10:00:00Z', (SELECT id FROM emitentes LIMIT 1), (SELECT id FROM clientes LIMIT 1), 'Emitente', '45723174000110', 'Cliente', '52998224725', 'SAIDA');",
            new { numero });

    [Fact]
    public async Task Mesma_data_de_emissao_desempata_pelo_id_decrescente()
    {
        await Nfe(1); await Nfe(2); await Nfe(3);

        using var s = _db.NovaSessao();
        var r = await new NfesRepository(s).ObterNfes();

        Assert.Equal(new[] { 3, 2, 1 }, r.Itens.Select(n => n.Id).ToArray());
    }
}
