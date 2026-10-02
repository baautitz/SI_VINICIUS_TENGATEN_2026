using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Backend.Core.Features.NFe.Entities;

namespace Backend.Core.Features.NFe.Services;

// XML NF-e 4.0 simplificado de saída (sem assinatura nem SEFAZ). Impostos fixos: ICMSSN102, PIS/COFINS não tributados.
public static partial class NfeXmlGerador
{
    private static readonly XNamespace Ns = "http://www.portalfiscal.inf.br/nfe";

    public static string Gerar(Nfes nfe)
    {
        var chave = nfe.ChaveAcesso ?? throw new InvalidOperationException("NF-e sem chave de acesso.");
        var parceiroDoc = nfe.ClienteCpfCnpj.Valor;

        var det = nfe.NfesItens.OrderBy(i => i.NumeroItem).Select(i => E("det", new XAttribute("nItem", i.NumeroItem),
            E("prod",
                E("cProd", i.Sku.Sku), E("cEAN", string.IsNullOrWhiteSpace(i.Sku.GtinEan) ? "SEM GTIN" : i.Sku.GtinEan),
                E("xProd", i.DescricaoItem), E("NCM", "00000000"), E("CFOP", "5102"),
                E("uCom", i.UnidadeMedida.Sigla), E("qCom", N(i.Quantidade, 4)), E("vUnCom", N(i.ValorUnitario, 4)),
                E("vProd", N(i.Quantidade * i.ValorUnitario, 2)),
                E("cEANTrib", "SEM GTIN"), E("uTrib", i.UnidadeMedida.Sigla), E("qTrib", N(i.Quantidade, 4)), E("vUnTrib", N(i.ValorUnitario, 4)),
                i.ValorDesconto > 0 ? E("vDesc", N(i.ValorDesconto, 2)) : null,
                E("indTot", "1")),
            E("imposto",
                E("ICMS", E("ICMSSN102", E("orig", "0"), E("CSOSN", "102"))),
                E("PIS", E("PISNT", E("CST", "07"))),
                E("COFINS", E("COFINSNT", E("CST", "07"))))));

        var infNFe = E("infNFe", new XAttribute("Id", "NFe" + chave), new XAttribute("versao", "4.00"),
            E("ide",
                E("cUF", chave[..2]), E("cNF", chave.Substring(35, 8)), E("natOp", "VENDA"), E("mod", "55"),
                E("serie", nfe.Serie.ToString(CultureInfo.InvariantCulture)), E("nNF", nfe.Numero.ToString(CultureInfo.InvariantCulture)),
                E("dhEmi", new DateTimeOffset(nfe.DataEmissao.ToUniversalTime()).ToOffset(NfeChaveAcesso.FusoBrasilia).ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture)),
                E("tpNF", "1"), E("idDest", "1"), E("tpImp", "1"), E("tpEmis", "1"), E("cDV", chave[43].ToString()),
                E("tpAmb", "2"), E("finNFe", "1"), E("indFinal", "1"), E("indPres", "1"), E("procEmi", "0"), E("verProc", "TCC-1.0")),
            E("emit",
                Doc(nfe.EmitenteCpfCnpj.Valor), E("xNome", nfe.EmitenteNomeRazaosocial), E("xFant", nfe.EmitenteApelidoNomefantasia),
                E("enderEmit", E("xLgr", nfe.EmitenteEndereco), E("xBairro", nfe.EmitenteBairro), E("fone", Digitos(nfe.EmitenteTelefone))),
                E("IE", Digitos(nfe.EmitenteRgIe?.Valor)), E("CRT", "1")),
            E("dest",
                Doc(parceiroDoc), E("xNome", nfe.ClienteNomeRazaosocial),
                E("enderDest", E("xLgr", nfe.ClienteEndereco), E("xBairro", nfe.ClienteBairro), E("fone", Digitos(nfe.ClienteTelefone))),
                E("indIEDest", "9"), E("email", nfe.ClienteEmail)),
            det,
            E("total", E("ICMSTot",
                E("vBC", "0.00"), E("vICMS", "0.00"), E("vICMSDeson", "0.00"), E("vFCP", "0.00"), E("vBCST", "0.00"), E("vST", "0.00"),
                E("vFCPST", "0.00"), E("vFCPSTRet", "0.00"),
                E("vProd", N(nfe.ValorProdutos, 2)), E("vFrete", N(nfe.ValorFrete, 2)), E("vSeg", N(nfe.ValorSeguro, 2)),
                E("vDesc", N(nfe.ValorDesconto, 2)), E("vII", "0.00"), E("vIPI", "0.00"), E("vIPIDevol", "0.00"),
                E("vPIS", "0.00"), E("vCOFINS", "0.00"), E("vOutro", N(nfe.ValorOutrasDespesas, 2)), E("vNF", N(nfe.ValorTotal, 2)))),
            E("transp", E("modFrete", "9")),
            E("pag", nfe.NfesPagamentos.Select(p => E("detPag",
                E("indPag", p.IndicadorPagamento == Entities.Enums.IndicadorPagamento.A_VISTA ? "0" : "1"),
                E("tPag", DoisDigitos().IsMatch(p.MetodosPagamento.Codigo) ? p.MetodosPagamento.Codigo : "99"),
                DoisDigitos().IsMatch(p.MetodosPagamento.Codigo) ? null : E("xPag", p.MetodosPagamento.Descricao),
                E("vPag", N(p.ValorPagamento, 2))))));

        var doc = new XDocument(new XDeclaration("1.0", "UTF-8", null), E("NFe", infNFe));
        return doc.Declaration + Environment.NewLine + doc.Root!.ToString(SaveOptions.DisableFormatting);
    }

    // Elemento no namespace da NF-e; filhos nulos (e elementos de texto vazio) somem.
    private static XElement? E(string nome, params object?[] conteudo)
    {
        if (conteudo.Length == 1 && conteudo[0] is null) return null;
        if (conteudo.Length == 1 && conteudo[0] is string s && string.IsNullOrWhiteSpace(s)) return null;
        var el = new XElement(Ns + nome, conteudo.Where(c => c is not null));
        return el.IsEmpty ? null : el;
    }

    private static XElement? Doc(string documento) => E(documento.Length == 11 ? "CPF" : "CNPJ", documento);
    private static string N(decimal v, int casas) => Math.Round(v, casas, MidpointRounding.AwayFromZero).ToString("F" + casas, CultureInfo.InvariantCulture);
    private static string? Digitos(string? v) => v is null ? null : new string(v.Where(char.IsAsciiDigit).ToArray());

    [GeneratedRegex(@"^\d{2}$")]
    private static partial Regex DoisDigitos();
}
