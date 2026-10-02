using Backend.Core.Common.Exceptions;
using Backend.Core.Common.ValueObjects;
using Backend.Core.Features.Catalogo.Entities;
using Backend.Core.Features.Financeiro.Entities;
using Backend.Core.Features.Localizacao.Entities;
using Backend.Core.Features.NFe.Entities;
using Backend.Core.Features.NFe.Entities.Enums;
using Backend.Core.Features.NFe.Services;
using Backend.Core.Features.Parceiros.Entities;
using Backend.Core.Features.Parceiros.Enums;

namespace Backend.Tests;

public class NfeSaidaTests
{
    private const string Cnpj = "11222333000181";

    [Fact]
    public void Dv_da_chave_conhecida()
    {
        // Corpo de 43 dígitos com DV calculado à mão (módulo 11, pesos 2..9): 3526101122233300018155001000000021112345678 -> 3 (conferido com cálculo independente em Python).
        Assert.Equal(3, NfeChaveAcesso.CalcularDv("3526101122233300018155001000000021112345678"));
        Assert.Equal(0, NfeChaveAcesso.CalcularDv("0000000000000000000000000000000000000000000")); // resto < 2 -> 0
    }

    [Fact]
    public void Gerar_monta_44_digitos_com_dv_valido()
    {
        var chave = NfeChaveAcesso.Gerar("SP", new DateTime(2026, 10, 2), Cnpj, 1, 21, 12345678);

        Assert.Equal("35261011222333000181550010000000211123456783", chave);
        NfeXmlParser.ValidarChave(chave);
        Assert.Throws<DomainException>(() => NfeChaveAcesso.Gerar("XX", DateTime.UtcNow, Cnpj, 1, 1, 1));
    }

    private static Nfes NfeDeSaida()
    {
        var brasil = Paises.Criar(new Ddi("55"), "BRA", "BRL", "R$", "Brasil").Data!;
        var emitente = new Emitentes(TipoPessoa.JURIDICA, "Minha Loja", Documento.Criar(Cnpj, "BRA", TipoPessoa.JURIDICA), brasil, logradouro: "Rua A", numero: "10");
        var cliente = new Clientes(TipoPessoa.FISICA, "Maria", Documento.Criar("52998224725", "BRA", TipoPessoa.FISICA), brasil);
        var un = new UnidadesMedida("UN", "Unidade", "CONTAGEM", true);
        var sku = new Skus("SKU-1", 10m, produto: new Produtos("Parafuso", null, new Categorias("C"), new Marcas("M"), un));
        var itens = new[] { new NfesItens(1, "Parafuso", 2.5m, 10m, 5m, sku, un) };
        var pagamentos = new[] { new NfesPagamentos(IndicadorPagamento.A_VISTA, 20m, new MetodosPagamentos("01", "Dinheiro")) };
        var chave = NfeChaveAcesso.Gerar("SP", new DateTime(2026, 10, 2), Cnpj, 1, 21, 12345678);

        var nfe = new Nfes(21, 1, new DateTime(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc), emitente, cliente, TipoOperacao.SAIDA,
            itens, pagamentos, chave, valorDesconto: 5m, vendaId: 7);
        nfe.Emitir(NfeXmlGerador.Gerar(nfe));
        return nfe;
    }

    [Fact]
    public void Xml_gerado_e_bem_formado_e_relido_pelo_parser()
    {
        var nfe = NfeDeSaida();
        var d = NfeXmlParser.Parse(nfe.XmlDocumento!);

        Assert.Equal(StatusNfe.EMITIDA, nfe.StatusNfe);
        Assert.Equal(nfe.ChaveAcesso, d.ChaveAcesso);
        Assert.Equal(21, d.Numero);
        Assert.Equal(Cnpj, d.Emitente.Documento);
        Assert.Equal("52998224725", d.Destinatario!.Documento);
        var item = Assert.Single(d.Itens);
        Assert.Equal("SKU-1", item.CodigoProduto);
        Assert.Equal(2.5m, item.Quantidade);
        Assert.Equal(5m, item.ValorDesconto);
        Assert.Equal(20m, d.Totais.Nota);
        Assert.Equal(5m, d.Totais.Desconto);
    }

    [Fact]
    public void Cancelar_exige_justificativa_e_so_vale_uma_vez()
    {
        var nfe = NfeDeSaida();

        Assert.Throws<DomainException>(() => nfe.Cancelar("curta"));
        Assert.Equal(StatusNfe.EMITIDA, nfe.StatusNfe);

        nfe.Cancelar("Cancelamento por erro de digitacao");
        Assert.Equal(StatusNfe.CANCELADA, nfe.StatusNfe);
        Assert.NotNull(nfe.DataCancelamento);
        Assert.Equal("CANCELAMENTO POR ERRO DE DIGITACAO", nfe.JustificativaCancelamento);
        Assert.Throws<DomainException>(() => nfe.Cancelar("Cancelamento por erro de digitacao"));
    }
}
