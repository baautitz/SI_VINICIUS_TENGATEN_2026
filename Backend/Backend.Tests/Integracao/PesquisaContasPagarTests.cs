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

    private async Task<int> Cliente(string nome)
    {
        using var s = _db.NovaSessao();
        return await s.Connection.ExecuteScalarAsync<int>(
            "INSERT INTO clientes (tipo_pessoa, nome_razaosocial, cpf_cnpj, nacionalidade_id) VALUES ('FISICA', @nome, '52998224725', (SELECT id FROM paises LIMIT 1)) RETURNING id;",
            new { nome });
    }

    private async Task<int> Transportadora(string nome)
    {
        using var s = _db.NovaSessao();
        return await s.Connection.ExecuteScalarAsync<int>(
            "INSERT INTO transportadoras (tipo_pessoa, nome_razaosocial, cpf_cnpj, nacionalidade_id) VALUES ('JURIDICA', @nome, '45723174000110', (SELECT id FROM paises LIMIT 1)) RETURNING id;",
            new { nome });
    }

    [Theory]
    [InlineData("MANUAL")]
    [InlineData("DEVOLUCAO_VENDA")]
    public async Task Titulo_so_com_cliente_e_lido_e_listado_com_o_nome_do_cliente(string origem)
    {
        var cliente = await Cliente("Maria Cliente");
        await _db.Executar("INSERT INTO contas_pagar (origem_tipo, origem_id, numero_documento, numero_parcela, total_parcelas, cliente_id, descricao, data_vencimento, valor) VALUES (@origem::origem_titulo_financeiro_enum, CASE WHEN @origem = 'MANUAL' THEN NULL ELSE 1 END, 'CLI-001', 1, 1, @cliente, 'Reembolso', CURRENT_DATE, 100);",
            new { origem, cliente });

        using var s = _db.NovaSessao();
        var r = await new ContasPagarRepository(s).PesquisarContasPagar("");

        var titulo = Assert.Single(r.Itens);
        Assert.Equal("Maria Cliente", titulo.ClienteNome);
        Assert.Null(titulo.Fornecedor);
    }

    [Theory]
    [InlineData("MANUAL")]
    [InlineData("FRETE_COMPRA")]
    public async Task Titulo_so_com_transportadora_e_lido_e_listado_com_o_nome_da_transportadora(string origem)
    {
        var transportadora = await Transportadora("Rapido Transportes");
        await _db.Executar("INSERT INTO contas_pagar (origem_tipo, origem_id, numero_documento, numero_parcela, total_parcelas, transportadora_id, descricao, data_vencimento, valor) VALUES (@origem::origem_titulo_financeiro_enum, CASE WHEN @origem = 'MANUAL' THEN NULL ELSE 1 END, 'FRT-001', 1, 1, @transportadora, 'Frete', CURRENT_DATE, 100);",
            new { origem, transportadora });

        using var s = _db.NovaSessao();
        var r = await new ContasPagarRepository(s).PesquisarContasPagar("");

        var titulo = Assert.Single(r.Itens);
        Assert.Equal(transportadora, titulo.TransportadoraId);
        Assert.Equal("Rapido Transportes", titulo.TransportadoraNome);
        Assert.Null(titulo.Fornecedor);
    }

    [Fact]
    public async Task Listagem_mistura_os_tres_tipos_de_contraparte_sem_erro()
    {
        var f = await Fornecedor("Distribuidora Aurora", "11222333000181");
        var c = await Cliente("Maria Cliente");
        var t = await Transportadora("Rapido Transportes");
        await Titulo(f, "Compra", "MIX-001");
        await _db.Executar("INSERT INTO contas_pagar (numero_documento, numero_parcela, total_parcelas, cliente_id, descricao, data_vencimento, valor) VALUES ('MIX-002', 1, 1, @c, 'Reembolso', CURRENT_DATE, 100);", new { c });
        await _db.Executar("INSERT INTO contas_pagar (numero_documento, numero_parcela, total_parcelas, transportadora_id, descricao, data_vencimento, valor) VALUES ('MIX-003', 1, 1, @t, 'Frete', CURRENT_DATE, 100);", new { t });

        using var s = _db.NovaSessao();
        var r = await new ContasPagarRepository(s).PesquisarContasPagar("");

        Assert.Equal(3, r.Itens.Count());
    }

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
    public async Task Filtro_de_compra_traz_parcelas_de_compra_e_frete_so_daquela_compra()
    {
        var f = await Fornecedor("Fornecedor A", "11222333000181");
        await _db.Executar("INSERT INTO contas_pagar (origem_tipo, origem_id, numero_documento, numero_parcela, total_parcelas, fornecedor_id, descricao, data_vencimento, data_emissao, valor) VALUES ('COMPRA',7,'C-7',1,1,@f,'Titulo','2026-01-10','2026-01-01',50), ('FRETE_COMPRA',7,'F-7',1,1,@f,'Titulo','2026-01-10','2026-01-01',50), ('COMPRA',8,'C-8',1,1,@f,'Titulo','2026-01-10','2026-01-01',50), ('DEVOLUCAO_VENDA',7,'D-7',1,1,@f,'Titulo','2026-01-10','2026-01-01',50);", new { f });

        using var s = _db.NovaSessao();
        var r = await new ContasPagarRepository(s).PesquisarContasPagar("", filtro: new FiltroContasPagar(CompraId: 7));

        Assert.Equal(["C-7", "F-7"], r.Itens.Select(c => c.NumeroDocumento).Order());
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
