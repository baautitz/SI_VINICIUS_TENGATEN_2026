using Backend.Core.Features.Financeiro.DTOs;
using Backend.Infrastructure.PostgreSQL.Features.Financeiro;
using Dapper;

namespace Backend.Tests.Integracao;

[Collection(PostgresCollection.Nome)]
public class PesquisaContasReceberTests
{
    private readonly PostgresFixture _db;

    public PesquisaContasReceberTests(PostgresFixture db)
    {
        _db = db;
        _db.Limpar().GetAwaiter().GetResult();
    }

    private async Task<int> Cliente(string nome, string cpfCnpj)
    {
        using var s = _db.NovaSessao();
        return await s.Connection.ExecuteScalarAsync<int>(
            "INSERT INTO clientes (tipo_pessoa, nome_razaosocial, cpf_cnpj, nacionalidade_id) VALUES ('FISICA', @nome, @cpfCnpj, (SELECT id FROM paises LIMIT 1)) RETURNING id;",
            new { nome, cpfCnpj });
    }

    private Task Titulo(int clienteId, string descricao, string documento, decimal valor = 100m) =>
        _db.Executar("INSERT INTO contas_receber (numero_documento, numero_parcela, total_parcelas, cliente_id, descricao, data_vencimento, valor) VALUES (@documento, 1, 1, @clienteId, @descricao, CURRENT_DATE, @valor);",
            new { documento, clienteId, descricao, valor });

    [Fact]
    public async Task Termo_com_erro_de_digitacao_acento_e_cpf_formatado_encontram_o_titulo()
    {
        var maria = await Cliente("José Antônio Silva", "52998224725");
        var joao = await Cliente("João Pereira", "11144477735");
        await Titulo(maria, "Mensalidade de março", "MEN-001");
        await Titulo(joao, "Venda balcão", "VEN-001");

        using var s = _db.NovaSessao();
        var repo = new ContasReceberRepository(s);

        Assert.Equal(["MEN-001"], (await repo.PesquisarContasReceber("mensalidde")).Itens.Select(c => c.NumeroDocumento));
        Assert.Equal(["MEN-001"], (await repo.PesquisarContasReceber("JOSE antonio")).Itens.Select(c => c.NumeroDocumento));
        Assert.Equal(["VEN-001"], (await repo.PesquisarContasReceber("111.444.777-35")).Itens.Select(c => c.NumeroDocumento));
    }

    [Fact]
    public async Task Valor_com_virgula_e_ranking_por_documento_exato()
    {
        var c = await Cliente("Cliente Valor", "39053344705");
        await Titulo(c, "Parcela de serviço", "SRV-001", 1234.56m);
        await Titulo(c, "Outro serviço", "SRV-002", 80m);
        await Titulo(c, "Serviço", "SERVICO", 80m);

        using var s = _db.NovaSessao();
        var repo = new ContasReceberRepository(s);

        Assert.Equal(["SRV-001"], (await repo.PesquisarContasReceber("1234,56")).Itens.Select(x => x.NumeroDocumento));
        Assert.Equal("SERVICO", (await repo.PesquisarContasReceber("servico")).Itens.First().NumeroDocumento);
        Assert.Equal(["SRV-002"], (await repo.PesquisarContasReceber("servico", filtro: new FiltroContasReceber(NumeroDocumento: "SRV-002"))).Itens.Select(x => x.NumeroDocumento));
    }

    [Fact]
    public async Task Filtros_de_periodo_e_faixa_de_valor_reduzem_o_resultado()
    {
        var c = await Cliente("Cliente Filtros", "39053344705");
        await _db.Executar("INSERT INTO contas_receber (numero_documento, numero_parcela, total_parcelas, cliente_id, descricao, data_vencimento, data_emissao, valor) VALUES ('F-1',1,1,@c,'Titulo','2026-01-10','2026-01-01',50), ('F-2',1,1,@c,'Titulo','2026-02-10','2026-02-01',150), ('F-3',1,1,@c,'Titulo','2026-03-10','2026-03-01',250);", new { c });

        using var s = _db.NovaSessao();
        var repo = new ContasReceberRepository(s);
        string[] Docs(Backend.Core.Common.Results.ResultadoPaginado<Backend.Core.Features.Financeiro.Entities.ContasReceber> r) => r.Itens.Select(x => x.NumeroDocumento).ToArray();

        Assert.Equal(["F-2", "F-3"], Docs(await repo.PesquisarContasReceber("", filtro: new FiltroContasReceber(VencimentoDe: new DateTime(2026, 2, 1)))));
        Assert.Equal(["F-1", "F-2"], Docs(await repo.PesquisarContasReceber("", filtro: new FiltroContasReceber(VencimentoAte: new DateTime(2026, 2, 28)))));
        Assert.Equal(["F-2"], Docs(await repo.PesquisarContasReceber("", filtro: new FiltroContasReceber(EmissaoDe: new DateTime(2026, 2, 1), EmissaoAte: new DateTime(2026, 2, 28)))));
        Assert.Equal(["F-2"], Docs(await repo.PesquisarContasReceber("", filtro: new FiltroContasReceber(ValorMinimo: 100m, ValorMaximo: 200m))));
        Assert.Equal(["F-2"], Docs(await repo.PesquisarContasReceber("", filtro: new FiltroContasReceber(CpfCnpj: "390.533.447-05", ValorMinimo: 100m, ValorMaximo: 200m))));
        Assert.Empty(Docs(await repo.PesquisarContasReceber("", filtro: new FiltroContasReceber(CpfCnpj: "111.444.777-35"))));
    }

    [Fact]
    public async Task Parcelas_do_mesmo_documento_sao_listadas_separadas_e_sem_misturar_com_outra_origem()
    {
        var c = await Cliente("Cliente Parcelas", "39053344705");
        await _db.Executar(@"INSERT INTO contas_receber (origem_tipo, origem_id, numero_documento, numero_parcela, total_parcelas, cliente_id, descricao, data_vencimento, valor) VALUES
            ('VENDA', 7, '100', 1, 2, @c, 'Venda', '2026-01-10', 50), ('VENDA', 7, '100', 2, 2, @c, 'Venda', '2026-02-10', 50),
            ('MANUAL', NULL, '100', 1, 1, @c, 'Lançamento manual', '2026-01-15', 30);", new { c });

        using var s = _db.NovaSessao();
        var r = await new ContasReceberRepository(s).PesquisarContasReceber("", filtro: new FiltroContasReceber(OrigemTipo: Backend.Core.Features.Financeiro.Entities.Enums.OrigemTituloFinanceiro.VENDA, OrigemId: 7, NumeroDocumento: "100"));

        Assert.Equal([1, 2], r.Itens.Select(x => x.NumeroParcela));
        Assert.All(r.Itens, x => Assert.Equal(2, x.TotalParcelas));
    }
}
