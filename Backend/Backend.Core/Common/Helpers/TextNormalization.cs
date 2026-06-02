namespace Backend.Core.Common.Helpers;

public static class TextNormalization
{
    public static string Normalize(string value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();

    public static string? NormalizeOrNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

    public static bool EqualsIgnoreCase(string? a, string? b)
        => string.Equals(a?.Trim(), b?.Trim(), StringComparison.InvariantCultureIgnoreCase);
}
