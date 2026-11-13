using System.Text.RegularExpressions;

namespace Backend.Core.Common.Helpers;

/// <summary>Valida placas brasileiras: padrao antigo (ABC1234) e Mercosul (ABC1D23).</summary>
public static partial class PlacaVeiculo
{
    [GeneratedRegex("^[A-Z]{3}[0-9]([0-9]|[A-Z])[0-9]{2}$")]
    private static partial Regex PadraoPlaca();

    public static bool EhValida(string? placa)
    {
        if (string.IsNullOrWhiteSpace(placa)) return false;
        var normalizada = placa.Trim().Replace("-", "").ToUpperInvariant();
        return PadraoPlaca().IsMatch(normalizada);
    }
}
