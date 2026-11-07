using Backend.Core.Common.Exceptions;
using Backend.Core.Common.ValueObjects;
using Backend.Core.Features.Catalogo.Entities;
using Backend.Core.Features.Localizacao.Entities;
using Backend.Core.Features.NFe.Entities;
using Backend.Core.Features.NFe.Entities.Enums;
using Backend.Core.Features.NFe.Services;
using Backend.Core.Features.Parceiros.Entities;
using Backend.Core.Features.Parceiros.Enums;

namespace Backend.Tests.NFe.Unit;

public class NfeDevolucaoTests
{
    private const string Cnpj = "11222333000181";
    private static readonly string ChaveOriginal = NfeChaveAcesso.Gerar("SP", new DateTime(2026, 10, 2), Cnpj, 1, 20, 11111111);
    private static readonly string ChaveNova = NfeChaveAcesso.Gerar("SP", new DateTime(2026, 10, 3), Cnpj, 1, 21, 22222222);

    private static (Emitentes, Clientes, Fornecedores, List<NfesItens>) Cenario()
    {
        var brasil = Paises.Criar(new Ddi("55"), "BRA", "BRL", "R$", "Brasil").Data!;
        var emitente = new Emitentes(TipoPessoa.JURIDICA, "Minha Loja", Documento.Criar(Cnpj, "BRA", TipoPessoa.JURIDICA), brasil, logradouro: "Rua A", numero: "10");
        var cliente = new Clientes(TipoPessoa.FISICA, "Maria", Documento.Criar("52998224725", "BRA", TipoPessoa.FISICA), brasil);
        var fornecedor = new Fornecedores(TipoPessoa.JURIDICA, "Fornecedor X", Documento.Criar("45723174000110", "BRA", TipoPessoa.JURIDICA), brasil);
        var un = new UnidadesMedida("UN", "Unidade", "CONTAGEM", true);
        var sku = new Skus("SKU-1", 10m, produto: new Produtos("Parafuso", null, new Categorias("C"), new Marcas("M"), un));
        return (emitente, cliente, fornecedor, new List<NfesItens> { new(1, "Parafuso", 2.5m, 10m, 0m, sku, un) });
    }

    [Fact]
    public void Devolucao_de_venda_e_nota_de_entrada_propria_que_referencia_a_original()
    {
        var (emitente, cliente, _, itens) = Cenario();

        var nfe = Nfes.CriarDevolucao(FinalidadeNfe.DEVOLUCAO, TipoOperacao.ENTRADA, 21, 1, new DateTime(2026, 10, 3, 12, 0, 0, DateTimeKind.Utc),
            ChaveNova, emitente, cliente, null, itens, 5, ChaveOriginal, 9, null);

        Assert.Equal(StatusNfe.EMITIDA, nfe.StatusNfe);
        Assert.Equal(25m, nfe.ValorTotal);
        Assert.Equal(9, nfe.DevolucaoId);
        Assert.Null(nfe.VendaId);
        Assert.Contains("<finNFe>4</finNFe>", nfe.XmlDocumento);
        Assert.Contains("<tpNF>0</tpNF>", nfe.XmlDocumento);
        Assert.Contains("<CFOP>1202</CFOP>", nfe.XmlDocumento);
        Assert.Contains($"<NFref><refNFe>{ChaveOriginal}</refNFe></NFref>", nfe.XmlDocumento);
        Assert.Contains("<tPag>90</tPag>", nfe.XmlDocumento);

        var lida = NfeXmlParser.Parse(nfe.XmlDocumento!);
        Assert.Equal(ChaveNova, lida.ChaveAcesso);
        Assert.Equal(25m, lida.Totais.Nota);
    }

    [Fact]
    public void Devolucao_de_compra_e_nota_de_saida_com_o_fornecedor()
    {
        var (emitente, _, fornecedor, itens) = Cenario();

        var nfe = Nfes.CriarDevolucao(FinalidadeNfe.DEVOLUCAO, TipoOperacao.SAIDA, 21, 1, DateTime.UtcNow,
            ChaveNova, emitente, null, fornecedor, itens, 5, ChaveOriginal, null, 3);

        Assert.Equal(3, nfe.DevolucaoCompraId);
        Assert.Equal("FORNECEDOR X", nfe.ClienteNomeRazaosocial.ToUpperInvariant());
        Assert.Contains("<CFOP>5202</CFOP>", nfe.XmlDocumento);
        Assert.Contains("<tpNF>1</tpNF>", nfe.XmlDocumento);
        Assert.Contains("<finNFe>4</finNFe>", nfe.XmlDocumento);
    }

    [Fact]
    public void Estorno_da_devolucao_usa_finalidade_normal_no_xml_e_sentido_oposto()
    {
        var (emitente, cliente, _, itens) = Cenario();

        var nfe = Nfes.CriarDevolucao(FinalidadeNfe.ESTORNO_DEVOLUCAO, TipoOperacao.SAIDA, 22, 1, DateTime.UtcNow,
            ChaveNova, emitente, cliente, null, itens, 6, ChaveOriginal, 9, null);

        Assert.Contains("<finNFe>1</finNFe>", nfe.XmlDocumento);
        Assert.Contains("<CFOP>5949</CFOP>", nfe.XmlDocumento);
        Assert.Contains("ESTORNO DE DEVOLUCAO DE VENDA", nfe.XmlDocumento);
    }

    [Fact]
    public void Nota_de_devolucao_nao_pode_ser_cancelada()
    {
        var (emitente, cliente, _, itens) = Cenario();
        var nfe = Nfes.CriarDevolucao(FinalidadeNfe.DEVOLUCAO, TipoOperacao.SAIDA, 21, 1, DateTime.UtcNow,
            ChaveNova, emitente, cliente, null, itens, 5, ChaveOriginal, 9, null);

        Assert.Throws<DomainException>(() => nfe.Cancelar("Cancelamento por erro de digitacao"));
        Assert.Equal(StatusNfe.EMITIDA, nfe.StatusNfe);
    }

    [Fact]
    public void Exige_uma_contraparte_e_uma_devolucao()
    {
        var (emitente, cliente, fornecedor, itens) = Cenario();

        Assert.Throws<DomainException>(() => Nfes.CriarDevolucao(FinalidadeNfe.DEVOLUCAO, TipoOperacao.ENTRADA, 21, 1, DateTime.UtcNow,
            ChaveNova, emitente, cliente, fornecedor, itens, 5, ChaveOriginal, 9, null));
        Assert.Throws<DomainException>(() => Nfes.CriarDevolucao(FinalidadeNfe.DEVOLUCAO, TipoOperacao.ENTRADA, 21, 1, DateTime.UtcNow,
            ChaveNova, emitente, cliente, null, itens, 5, ChaveOriginal, null, null));
        Assert.Throws<DomainException>(() => Nfes.CriarDevolucao(FinalidadeNfe.NORMAL, TipoOperacao.ENTRADA, 21, 1, DateTime.UtcNow,
            ChaveNova, emitente, cliente, null, itens, 5, ChaveOriginal, 9, null));
    }
}
