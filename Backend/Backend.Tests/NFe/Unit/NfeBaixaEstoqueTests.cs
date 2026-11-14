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

// ADR 0006: a loja emite NF-e de baixa para si mesma (saída, CFOP 5927) e, no estorno da movimentação, a nota de
// estorno da baixa (entrada, CFOP 1949) referenciando a baixa.
public class NfeBaixaEstoqueTests
{
    private const string Cnpj = "11222333000181";
    private static readonly string ChaveBaixa = NfeChaveAcesso.Gerar("SP", new DateTime(2026, 10, 2), Cnpj, 1, 20, 11111111);
    private static readonly string ChaveEstorno = NfeChaveAcesso.Gerar("SP", new DateTime(2026, 10, 3), Cnpj, 1, 21, 22222222);

    private static (Emitentes, List<NfesItens>) Cenario()
    {
        var brasil = Paises.Criar(new Ddi("55"), "BRA", "BRL", "R$", "Brasil").Data!;
        var emitente = new Emitentes(TipoPessoa.JURIDICA, "Minha Loja", Documento.Criar(Cnpj, "BRA", TipoPessoa.JURIDICA), brasil, logradouro: "Rua A", numero: "10");
        var un = new UnidadesMedida("UN", "Unidade", "CONTAGEM", true);
        var sku = new Skus("SKU-1", 10m, produto: new Produtos("Parafuso", null, new Categorias("C"), new Marcas("M"), un));
        return (emitente, new List<NfesItens> { new(1, "Parafuso", 3m, 5m, 0m, sku, un) });
    }

    private static Nfes Baixa(Emitentes emitente, List<NfesItens> itens)
        => Nfes.CriarBaixaEstoque(FinalidadeNfe.BAIXA_ESTOQUE, 20, 1, new DateTime(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc),
            ChaveBaixa, emitente, itens, 7, null, null);

    [Fact]
    public void Baixa_e_nota_de_saida_cfop_5927_sem_nota_referenciada_e_com_a_loja_como_destinataria()
    {
        var (emitente, itens) = Cenario();

        var nfe = Baixa(emitente, itens);

        Assert.Equal(TipoOperacao.SAIDA, nfe.TipoOperacao);
        Assert.Equal(StatusNfe.EMITIDA, nfe.StatusNfe);
        Assert.Equal(FinalidadeNfe.BAIXA_ESTOQUE, nfe.Finalidade);
        Assert.Equal(7, nfe.MovimentacaoEstoqueId);
        Assert.Equal(15m, nfe.ValorTotal);
        Assert.Null(nfe.Cliente);
        Assert.Null(nfe.Fornecedor);
        Assert.Null(nfe.VendaId);
        Assert.Equal("MINHA LOJA", nfe.ClienteNomeRazaosocial);
        Assert.Equal(nfe.EmitenteCpfCnpj, nfe.ClienteCpfCnpj);
        Assert.Contains("<CFOP>5927</CFOP>", nfe.XmlDocumento);
        Assert.Contains("<natOp>BAIXA DE ESTOQUE</natOp>", nfe.XmlDocumento);
        Assert.Contains("<tpNF>1</tpNF>", nfe.XmlDocumento);
        Assert.Contains("<finNFe>1</finNFe>", nfe.XmlDocumento);
        Assert.Contains("<tPag>90</tPag>", nfe.XmlDocumento);
        Assert.DoesNotContain("<NFref>", nfe.XmlDocumento);

        var lida = NfeXmlParser.Parse(nfe.XmlDocumento!);
        Assert.Equal(ChaveBaixa, lida.ChaveAcesso);
        Assert.Equal(15m, lida.Totais.Nota);
    }

    [Fact]
    public void Estorno_da_baixa_e_nota_de_entrada_cfop_1949_que_referencia_a_baixa()
    {
        var (emitente, itens) = Cenario();

        var nfe = Nfes.CriarBaixaEstoque(FinalidadeNfe.ESTORNO_BAIXA_ESTOQUE, 21, 1, new DateTime(2026, 10, 3, 12, 0, 0, DateTimeKind.Utc),
            ChaveEstorno, emitente, itens, 7, 5, ChaveBaixa);

        Assert.Equal(TipoOperacao.ENTRADA, nfe.TipoOperacao);
        Assert.Equal(5, nfe.NfeReferenciadaId);
        Assert.Contains("<CFOP>1949</CFOP>", nfe.XmlDocumento);
        Assert.Contains("<tpNF>0</tpNF>", nfe.XmlDocumento);
        Assert.Contains($"<NFref><refNFe>{ChaveBaixa}</refNFe></NFref>", nfe.XmlDocumento);
    }

    [Fact]
    public void Baixa_nao_referencia_outra_nota()
    {
        var (emitente, itens) = Cenario();

        Assert.Throws<DomainException>(() => Nfes.CriarBaixaEstoque(FinalidadeNfe.BAIXA_ESTOQUE, 20, 1, DateTime.UtcNow,
            ChaveBaixa, emitente, itens, 7, 5, ChaveEstorno));
    }

    [Fact]
    public void Estorno_da_baixa_exige_a_nota_de_baixa_referenciada()
    {
        var (emitente, itens) = Cenario();

        Assert.Throws<DomainException>(() => Nfes.CriarBaixaEstoque(FinalidadeNfe.ESTORNO_BAIXA_ESTOQUE, 21, 1, DateTime.UtcNow,
            ChaveEstorno, emitente, itens, 7, null, null));
    }

    [Theory]
    [InlineData(FinalidadeNfe.NORMAL)]
    [InlineData(FinalidadeNfe.DEVOLUCAO)]
    [InlineData(FinalidadeNfe.ESTORNO_DEVOLUCAO)]
    public void Outras_finalidades_nao_passam_pela_fabrica_de_baixa(FinalidadeNfe finalidade)
    {
        var (emitente, itens) = Cenario();

        Assert.Throws<DomainException>(() => Nfes.CriarBaixaEstoque(finalidade, 20, 1, DateTime.UtcNow, ChaveBaixa, emitente, itens, 7, null, null));
    }

    [Fact]
    public void Baixa_sem_itens_e_recusada()
    {
        var (emitente, _) = Cenario();

        Assert.Throws<DomainException>(() => Baixa(emitente, new List<NfesItens>()));
    }

    [Fact]
    public void Nota_de_baixa_nao_se_cancela()
    {
        var (emitente, itens) = Cenario();
        var nfe = Baixa(emitente, itens);

        var erro = Assert.Throws<DomainException>(() => nfe.Cancelar("Justificativa com mais de quinze caracteres"));

        Assert.Contains("estorno da movimentação", erro.Message);
    }
}
