using Backend.Core.Features.Financeiro.DTOs;
using Backend.Infrastructure.PostgreSQL.Features.Financeiro;
using Dapper;

namespace Backend.Tests.Integracao;

[Collection(PostgresCollection.Nome)]
public class PesquisaContasPagarTests
{
    private readonly PostgresFixture _db;

    public PesquisaContasPagarTests(PostgresFixture db)
    {
        _db = db;
        _db.Limpar().GetAwaiter().GetResult();
    }

    private async Task<int> Fornecedor(string nome, string cpfCnpj)
    {
        using var s = _db.NovaSessao();
        return await s.Connection.ExecuteScalarAsync<int>(
            "INSERT INTO fornecedores (tipo_pessoa, nome_razaosocial, cpf_cnpj, nacionalidade_id) VALUES ('JURIDICA', @nome, @cpfCnpj, (SELECT id FROM paises LIMIT 1)) RETURNING id;",
            new { nome, cpfCnpj });
    }

    private Task Titulo(int fornecedorId, string descricao, string documento) =>
        _db.Executar("INSERT INTO contas_pagar (numero_documento, numero_parcela, total_parcelas, fornecedor_id, descricao, data_vencimento, valor) VALUES (@documento, 1, 1, @fornecedorId, @descricao, CURRENT_DATE, 100);",
            new { documento, fornecedorId, descricao });

    [Fact]
    public async Task Termo_com_erro_de_digitacao_encontra_o_titulo()
    {
        var f = await Fornecedor("Distribuidora Aurora", "11222333000181");
        await Titulo(f, "Aluguel do galpão", "ALG-001");
        await Titulo(f, "Energia elétrica", "ENE-001");

        using var s = _db.NovaSessao();
        var r = await new ContasPagarRepository(s).PesquisarContasPagar("alugel");

        Assert.Equal(["ALG-001"], r.Itens.Select(c => c.NumeroDocumento));
    }

    [Fact]
    public async Task Cnpj_formatado_encontra_titulo_do_fornecedor_e_ignora_acento_e_caixa()
    {
        var aurora = await Fornecedor("Distribuidora Aurora", "11222333000181");
        var outro = await Fornecedor("Comercial Bom Preço", "45723174000110");
        await Titulo(aurora, "Compra de material", "CMP-010");
        await Titulo(outro, "Compra de embalagem", "CMP-011");

        using var s = _db.NovaSessao();
        var repo = new ContasPagarRepository(s);

        Assert.Equal(["CMP-010"], (await repo.PesquisarContasPagar("11.222.333/0001-81")).Itens.Select(c => c.NumeroDocumento));
        Assert.Equal(["CMP-011"], (await repo.PesquisarContasPagar("PRECO")).Itens.Select(c => c.NumeroDocumento));
    }

    [Fact]
    public async Task Resultado_vem_ordenado_por_relevancia()
    {
        var f = await Fornecedor("Papelaria Central", "07526557000100");
        await Titulo(f, "Papel sulfite e toner", "REL-002");
        await Titulo(f, "Toner", "REL-003");
        await Titulo(f, "Material de escritório", "TONER");

        using var s = _db.NovaSessao();
        var r = await new ContasPagarRepository(s).PesquisarContasPagar("toner");

        Assert.Equal(["TONER", "REL-003", "REL-002"], r.Itens.Select(c => c.NumeroDocumento));
    }

    [Fact]
    public async Task Todas_as_palavras_do_termo_precisam_casar_e_combinam_com_filtros()
    {
        var a = await Fornecedor("Fornecedor A", "11444777000161");
        var b = await Fornecedor("Fornecedor B", "34028316000103");
        await Titulo(a, "Aluguel galpão norte", "AND-001");
        await Titulo(b, "Aluguel galpão sul", "AND-002");
        await Titulo(a, "Energia galpão norte", "AND-003");

        using var s = _db.NovaSessao();
        var repo = new ContasPagarRepository(s);

        Assert.Equal(["AND-001"], (await repo.PesquisarContasPagar("aluguel norte")).Itens.Select(c => c.NumeroDocumento));
        Assert.Equal(["AND-002"], (await repo.PesquisarContasPagar("aluguel", filtro: new FiltroContasPagar(FornecedorId: b))).Itens.Select(c => c.NumeroDocumento));
    }

    [Fact]
    public async Task Filtros_de_periodo_faixa_de_valor_e_cpf_cnpj_reduzem_o_resultado()
    {
        var a = await Fornecedor("Fornecedor A", "11222333000181");
        var b = await Fornecedor("Fornecedor B", "45723174000110");
        await _db.Executar("INSERT INTO contas_pagar (numero_documento, numero_parcela, total_parcelas, fornecedor_id, descricao, data_vencimento, data_emissao, valor) VALUES ('P-1',1,1,@a,'Titulo','2026-01-10','2026-01-01',50), ('P-2',1,1,@a,'Titulo','2026-02-10','2026-02-01',150), ('P-3',1,1,@b,'Titulo','2026-03-10','2026-03-01',250);", new { a, b });

        using var s = _db.NovaSessao();
        var repo = new ContasPagarRepository(s);
        string[] Docs(Backend.Core.Common.Results.ResultadoPaginado<Backend.Core.Features.Financeiro.Entities.ContasPagar> r) => r.Itens.Select(x => x.NumeroDocumento).ToArray();

        Assert.Equal(["P-2", "P-3"], Docs(await repo.PesquisarContasPagar("", filtro: new FiltroContasPagar(VencimentoDe: new DateTime(2026, 2, 1)))));
        Assert.Equal(["P-1", "P-2"], Docs(await repo.PesquisarContasPagar("", filtro: new FiltroContasPagar(VencimentoAte: new DateTime(2026, 2, 28)))));
        Assert.Equal(["P-2"], Docs(await repo.PesquisarContasPagar("", filtro: new FiltroContasPagar(EmissaoDe: new DateTime(2026, 2, 1), EmissaoAte: new DateTime(2026, 2, 28)))));
        Assert.Equal(["P-2"], Docs(await repo.PesquisarContasPagar("", filtro: new FiltroContasPagar(ValorMinimo: 100m, ValorMaximo: 200m))));
        Assert.Equal(["P-1", "P-2"], Docs(await repo.PesquisarContasPagar("", filtro: new FiltroContasPagar(CpfCnpj: "11.222.333/0001-81"))));
    }

    [Fact]
    public async Task Valor_decimal_com_ponto_nao_vira_so_digitos()
    {
        var f = await Fornecedor("Fornecedor Decimal", "11222333000181");
        await _db.Executar("INSERT INTO contas_pagar (numero_documento, numero_parcela, total_parcelas, fornecedor_id, descricao, data_vencimento, valor) VALUES ('DEC-1',1,1,@f,'Taxa',CURRENT_DATE,10.5);", new { f });

        using var s = _db.NovaSessao();
        var repo = new ContasPagarRepository(s);

        Assert.Equal(["DEC-1"], (await repo.PesquisarContasPagar("10.50")).Itens.Select(c => c.NumeroDocumento));
        Assert.Empty((await repo.PesquisarContasPagar("1050")).Itens);
    }
}
