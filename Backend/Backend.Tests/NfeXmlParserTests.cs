using Backend.Core.Common.Exceptions;
using Backend.Core.Features.NFe.Services;

namespace Backend.Tests;

public class NfeXmlParserTests
{
    private const string CnpjEmit = "11222333000181";

    // cUF 35 + AAMM 2605 + CNPJ + mod 55 + serie 001 + nNF 000001234 + tpEmis 1 + cNF 12345678 + DV (módulo 11).
    private static string Chave(string cnpj = CnpjEmit, string mod = "55")
    {
        var b = "352605" + cnpj + mod + "001" + "000001234" + "1" + "12345678";
        var soma = 0;
        for (int i = 42, peso = 2; i >= 0; i--, peso = peso == 9 ? 2 : peso + 1) soma += (b[i] - '0') * peso;
        var r = soma % 11;
        return b + (r < 2 ? 0 : 11 - r);
    }

    private static string Nfe(string chave, string ns = " xmlns=\"http://www.portalfiscal.inf.br/nfe\"", string mod = "55") => $$"""
        <NFe{{ns}}><infNFe Id="NFe{{chave}}" versao="4.00">
          <ide><mod>{{mod}}</mod><serie>1</serie><nNF>1234</nNF><dhEmi>2026-05-10T10:00:00-03:00</dhEmi></ide>
          <emit><CNPJ>{{CnpjEmit}}</CNPJ><xNome>Fornecedor Ltda</xNome><xFant>Forn</xFant>
            <enderEmit><xLgr>Rua A</xLgr><nro>10</nro><xBairro>Centro</xBairro><fone>1133334444</fone></enderEmit><IE>123456789</IE></emit>
          <dest><CNPJ>99888777000166</CNPJ><xNome>Minha Loja</xNome><email>loja@x.com</email></dest>
          <det nItem="1"><prod><cProd>A-1</cProd><cEAN>7891234567895</cEAN><xProd>Parafuso</xProd><uCom>UN</uCom>
            <qCom>10.0000</qCom><vUnCom>5.5000</vUnCom><vProd>55.00</vProd><vDesc>5.00</vDesc></prod></det>
          <det nItem="2"><prod><cProd>B-2</cProd><cEAN>SEM GTIN</cEAN><xProd>Porca</xProd><uCom>CX</uCom>
            <qCom>2.5000</qCom><vUnCom>10.0000</vUnCom><vProd>25.00</vProd></prod></det>
          <total><ICMSTot><vProd>80.00</vProd><vDesc>5.00</vDesc><vFrete>10.00</vFrete><vSeg>1.00</vSeg><vOutro>2.00</vOutro><vNF>88.00</vNF></ICMSTot></total>
          <transp><modFrete>1</modFrete><transporta><CNPJ>11444777000161</CNPJ><xNome>Transp SA</xNome></transporta></transp>
          <cobr><dup><nDup>001</nDup><dVenc>2026-06-10</dVenc><vDup>44.00</vDup></dup><dup><nDup>002</nDup><dVenc>2026-07-10</dVenc><vDup>44.00</vDup></dup></cobr>
          <pag><detPag><tPag>15</tPag><vPag>88.00</vPag></detPag></pag>
          <infAdic><infCpl>Pedido 77</infCpl></infAdic>
        </infNFe></NFe>
        """;

    [Fact]
    public void Le_nfeProc_com_namespace_itens_sem_gtin_e_parcelas()
    {
        var d = NfeXmlParser.Parse($"<nfeProc xmlns=\"http://www.portalfiscal.inf.br/nfe\">{Nfe(Chave(), "")}<protNFe/></nfeProc>");

        Assert.Equal(Chave(), d.ChaveAcesso);
        Assert.Equal(1234, d.Numero);
        Assert.Equal((short)1, d.Serie);
        Assert.Equal(new DateTime(2026, 5, 10, 13, 0, 0, DateTimeKind.Utc), d.DataEmissao);
        Assert.Equal(new DateTime(2026, 5, 10, 10, 0, 0), d.DataEmissaoLocal);
        Assert.Equal(CnpjEmit, d.Emitente.Documento);
        Assert.Equal("Rua A", d.Emitente.Logradouro);
        Assert.Equal("1133334444", d.Emitente.Telefone);
        Assert.Equal("99888777000166", d.Destinatario!.Documento);
        Assert.Equal(2, d.Itens.Count);
        Assert.Equal("7891234567895", d.Itens[0].Ean);
        Assert.Equal(5m, d.Itens[0].ValorDesconto);
        Assert.Equal("SEM GTIN", d.Itens[1].Ean);
        Assert.Equal(0m, d.Itens[1].ValorDesconto);
        Assert.Equal(2.5m, d.Itens[1].Quantidade);
        Assert.Equal(88m, d.Totais.Nota);
        Assert.Equal(10m, d.Totais.Frete);
        Assert.Equal(1, d.ModalidadeFrete);
        Assert.Equal("11444777000161", d.Transportadora!.Documento);
        Assert.Equal(new[] { 1, 2 }, d.Parcelas.Select(p => p.Numero));
        Assert.Equal(new DateTime(2026, 7, 10), d.Parcelas[1].Vencimento.Date);
        Assert.Equal(88m, d.Parcelas.Sum(p => p.Valor));
        Assert.Equal("Pedido 77", d.InformacoesComplementares);
    }

    [Fact]
    public void Le_nfe_sem_nfeProc_e_sem_namespace()
    {
        var d = NfeXmlParser.Parse(Nfe(Chave(), ""));
        Assert.Equal(Chave(), d.ChaveAcesso);
    }

    [Fact]
    public void Rejeita_digito_verificador_invalido()
    {
        var chave = Chave();
        var ruim = chave[..43] + (chave[43] == '0' ? '1' : '0');
        var ex = Assert.Throws<DomainException>(() => NfeXmlParser.Parse(Nfe(ruim)));
        Assert.Contains("dígito verificador", ex.Message);
    }

    [Fact]
    public void Rejeita_cnpj_da_chave_diferente_do_emitente()
        => Assert.Throws<DomainException>(() => NfeXmlParser.Parse(Nfe(Chave("99888777000166"))));

    [Fact]
    public void Rejeita_modelo_diferente_de_55()
        => Assert.Throws<DomainException>(() => NfeXmlParser.Parse(Nfe(Chave(mod: "65"), mod: "65")));

    [Fact]
    public void Rejeita_evento_e_resumo()
    {
        Assert.Throws<DomainException>(() => NfeXmlParser.Parse("<procEventoNFe><evento/></procEventoNFe>"));
        Assert.Throws<DomainException>(() => NfeXmlParser.Parse("<resNFe><chNFe/></resNFe>"));
    }

    [Fact]
    public void Rejeita_dtd_xxe_e_xml_gigante()
    {
        Assert.Throws<DomainException>(() => NfeXmlParser.Parse("<!DOCTYPE NFe [<!ENTITY x SYSTEM \"file:///etc/passwd\">]><NFe>&x;</NFe>"));
        Assert.Throws<DomainException>(() => NfeXmlParser.Parse("<NFe>" + new string('a', NfeXmlParser.TamanhoMaximoBytes) + "</NFe>"));
    }
}
