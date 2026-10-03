using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Backend.Core.Common.Exceptions;
using Backend.Core.Common.Helpers;
using Backend.Core.Features.NFe.DTOs;

namespace Backend.Core.Features.NFe.Services;

// Lê o XML de uma NF-e modelo 55 (layout 4.0, com ou sem nfeProc). Ignora namespace (LocalName).
// Falhas de formato/regra viram DomainException (400).
public static class NfeXmlParser
{
    public const int TamanhoMaximoBytes = 2 * 1024 * 1024;

    public static NfeXmlDados Parse(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            throw new DomainException("XML da NF-e não informado.");
        if (Encoding.UTF8.GetByteCount(xml) > TamanhoMaximoBytes)
            throw new DomainException("XML maior que o limite permitido (2 MB).");

        XDocument doc;
        try
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = TamanhoMaximoBytes
            };
            using var reader = XmlReader.Create(new StringReader(xml), settings);
            doc = XDocument.Load(reader);
        }
        catch (XmlException ex)
        {
            throw new DomainException($"XML inválido: {ex.Message}");
        }

        var raiz = doc.Root!;
        var nfe = raiz.Name.LocalName switch
        {
            "nfeProc" => Filho(raiz, "NFe"),
            "NFe" => raiz,
            _ => throw new DomainException($"Documento '{raiz.Name.LocalName}' não é uma NF-e (eventos e resumos não são aceitos).")
        };
        var inf = Filho(nfe, "infNFe") ?? throw new DomainException("Elemento infNFe não encontrado.");

        var chave = (inf.Attribute("Id")?.Value ?? "").Replace("NFe", "");
        ValidarChave(chave);

        var ide = Obrigatorio(inf, "ide");
        var modelo = Texto(ide, "mod");
        if (modelo != "55" || chave.Substring(20, 2) != "55")
            throw new DomainException("Somente NF-e modelo 55 é aceita.");

        var emit = Obrigatorio(inf, "emit");
        var emitente = Parte(emit, "enderEmit");
        if (emitente.Documento is { Length: 14 } cnpj && cnpj != chave.Substring(6, 14))
            throw new DomainException("O CNPJ da chave de acesso não confere com o emitente do XML.");

        var dest = Filho(inf, "dest");
        var transp = Filho(inf, "transp");
        var tot = Obrigatorio(Obrigatorio(inf, "total"), "ICMSTot");

        var itens = inf.Elements().Where(e => e.Name.LocalName == "det").Select(det =>
        {
            var prod = Obrigatorio(det, "prod");
            var ean = Texto(prod, "cEAN");
            return new NfeXmlItem(
                (int)Decimal(det.Attribute("nItem")?.Value, "nItem"),
                TextNormalization.Truncate(Texto(prod, "cProd") ?? throw new DomainException("Item sem cProd."), 60)!,
                ean,
                Texto(prod, "xProd") ?? throw new DomainException("Item sem xProd."),
                Texto(prod, "uCom") ?? "",
                Decimal(Texto(prod, "qCom"), "qCom"),
                Decimal(Texto(prod, "vUnCom"), "vUnCom"),
                Decimal(Texto(prod, "vDesc"), "vDesc", opcional: true),
                Decimal(Texto(prod, "vProd"), "vProd"));
        }).ToList();
        if (itens.Count == 0)
            throw new DomainException("A NF-e não possui itens.");

        var parcelas = Filho(inf, "cobr")?.Elements().Where(e => e.Name.LocalName == "dup")
            .Select((dup, i) => new NfeXmlParcela(
                i + 1,
                Data(Texto(dup, "dVenc") ?? throw new DomainException("Parcela (dup) sem dVenc.")),
                Decimal(Texto(dup, "vDup"), "vDup"))).ToList() ?? new();

        var emissao = Texto(ide, "dhEmi") ?? Texto(ide, "dEmi") ?? throw new DomainException("Data de emissão não encontrada.");

        return new NfeXmlDados(
            chave,
            (int)Decimal(Texto(ide, "nNF"), "nNF"),
            (short)Decimal(Texto(ide, "serie"), "serie"),
            DateTimeOffset.Parse(emissao, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).UtcDateTime,
            emitente,
            dest is null ? null : Parte(dest, "enderDest"),
            itens,
            new NfeXmlTotais(
                Decimal(Texto(tot, "vProd"), "vProd"), Decimal(Texto(tot, "vDesc"), "vDesc", true),
                Decimal(Texto(tot, "vFrete"), "vFrete", true), Decimal(Texto(tot, "vSeg"), "vSeg", true),
                Decimal(Texto(tot, "vOutro"), "vOutro", true), Decimal(Texto(tot, "vNF"), "vNF")),
            transp is null || Texto(transp, "modFrete") is not { } mod ? null : int.Parse(mod, CultureInfo.InvariantCulture),
            transp is null || Filho(transp, "transporta") is not { } t ? null : Parte(t, null),
            parcelas,
            Texto(Filho(inf, "infAdic"), "infCpl"),
            DateTimeOffset.Parse(emissao, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).DateTime,
            Transporte(transp));
    }

    private static NfeXmlTransporte? Transporte(XElement? transp)
    {
        if (transp is null) return null;
        var vols = transp.Elements().Where(e => e.Name.LocalName == "vol").ToList();
        var veic = Filho(transp, "veicTransp");
        if (vols.Count == 0 && veic is null) return null;
        decimal? Soma(string campo) => vols.Select(v => Texto(v, campo)).Where(x => x is not null).ToList() is { Count: > 0 } l
            ? l.Sum(x => Decimal(x, campo)) : null;
        return new NfeXmlTransporte(
            Soma("qVol") is { } q ? (int)q : null,
            vols.Select(v => Texto(v, "esp")).FirstOrDefault(x => x is not null),
            vols.Select(v => Texto(v, "marca")).FirstOrDefault(x => x is not null),
            vols.Select(v => Texto(v, "nVol")).FirstOrDefault(x => x is not null),
            Soma("pesoL"), Soma("pesoB"),
            Texto(veic, "placa"), Texto(veic, "UF"), Texto(veic, "RNTC"));
    }

    // 44 dígitos; DV = módulo 11 (pesos 2..9 da direita para a esquerda).
    public static void ValidarChave(string chave)
    {
        if (chave.Length != 44 || !chave.All(char.IsAsciiDigit))
            throw new DomainException("Chave de acesso inválida: deve ter 44 dígitos.");

        var dv = NfeChaveAcesso.CalcularDv(chave[..43]);
        if (dv != chave[43] - '0')
            throw new DomainException("Chave de acesso inválida: dígito verificador não confere.");
    }

    private static NfeXmlParte Parte(XElement e, string? endereco)
    {
        var end = endereco is null ? null : Filho(e, endereco);
        var doc = Texto(e, "CNPJ") ?? Texto(e, "CPF");
        return new NfeXmlParte(
            doc, Texto(e, "xNome") ?? "", Texto(e, "xFant"), Texto(e, "IE"),
            Texto(end, "xLgr") ?? Texto(e, "xEnder"), Texto(end, "nro"), Texto(end, "xBairro"), Texto(end, "fone"), Texto(e, "email"));
    }

    private static XElement? Filho(XElement? pai, string nome) => pai?.Elements().FirstOrDefault(e => e.Name.LocalName == nome);

    private static XElement Obrigatorio(XElement pai, string nome)
        => Filho(pai, nome) ?? throw new DomainException($"Elemento {nome} não encontrado no XML.");

    private static string? Texto(XElement? pai, string nome)
    {
        var v = Filho(pai, nome)?.Value.Trim();
        return string.IsNullOrEmpty(v) ? null : v;
    }

    private static decimal Decimal(string? valor, string campo, bool opcional = false)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return opcional ? 0m : throw new DomainException($"Campo {campo} não informado no XML.");
        return decimal.TryParse(valor, NumberStyles.Number, CultureInfo.InvariantCulture, out var d)
            ? d : throw new DomainException($"Campo {campo} com valor inválido: '{valor}'.");
    }

    private static DateTime Data(string valor)
        => DateTime.TryParse(valor, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var d)
            ? d : throw new DomainException($"Data inválida no XML: '{valor}'.");
}
