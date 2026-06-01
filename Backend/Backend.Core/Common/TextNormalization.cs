namespace Backend.Core.Common;

public static class TextNormalization
{
    public static string Normalize(string value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();

    public static string? NormalizeOrNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

    public static bool EqualsIgnoreCase(string? a, string? b)
        => string.Equals(a?.Trim(), b?.Trim(), StringComparison.InvariantCultureIgnoreCase);

    public static string NormalizeDocument(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return new string(value.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
    }

    public static string? NormalizeDocumentOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return new string(value.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
    }
}
