using System.Globalization;
using Backend.Core.Common.Exceptions;

namespace Backend.Core.Features.NFe.Services;

// Chave de 44 dígitos: cUF(2) AAMM(4) CNPJ(14) mod(2) serie(3) nNF(9) tpEmis(1) cNF(8) DV(1).
public static class NfeChaveAcesso
{
    private static readonly Dictionary<string, string> CodigoUf = new()
    {
        ["RO"] = "11", ["AC"] = "12", ["AM"] = "13", ["RR"] = "14", ["PA"] = "15", ["AP"] = "16", ["TO"] = "17",
        ["MA"] = "21", ["PI"] = "22", ["CE"] = "23", ["RN"] = "24", ["PB"] = "25", ["PE"] = "26", ["AL"] = "27", ["SE"] = "28", ["BA"] = "29",
        ["MG"] = "31", ["ES"] = "32", ["RJ"] = "33", ["SP"] = "35",
        ["PR"] = "41", ["SC"] = "42", ["RS"] = "43",
        ["MS"] = "50", ["MT"] = "51", ["GO"] = "52", ["DF"] = "53"
    };

    // Brasília (-03:00, sem horário de verão desde 2019): AAMM da chave e dhEmi usam o horário local, não UTC.
    public static readonly TimeSpan FusoBrasilia = TimeSpan.FromHours(-3);

    public static string Gerar(string uf, DateTime emissao, string cnpj, short serie, int numero, int codigoNumerico)
    {
        if (!CodigoUf.TryGetValue(uf.Trim().ToUpperInvariant(), out var cUf))
            throw new DomainException($"UF '{uf}' inválida para gerar a chave de acesso.");
        if (cnpj.Length != 14 || !cnpj.All(char.IsAsciiDigit))
            throw new DomainException("A chave de acesso exige emitente com CNPJ de 14 dígitos.");

        var corpo = string.Create(CultureInfo.InvariantCulture,
            $"{cUf}{emissao:yyMM}{cnpj}55{serie:000}{numero:000000000}1{codigoNumerico:00000000}");
        return corpo + CalcularDv(corpo);
    }

    // Módulo 11, pesos 2..9 da direita para a esquerda sobre os 43 primeiros dígitos.
    public static int CalcularDv(string corpo43)
    {
        var soma = 0;
        for (int i = corpo43.Length - 1, peso = 2; i >= 0; i--, peso = peso == 9 ? 2 : peso + 1)
            soma += (corpo43[i] - '0') * peso;
        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }
}
