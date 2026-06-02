namespace Backend.Core.Common.ValueObjects;

public abstract class Documento : IEquatable<Documento>
{
    public string Valor { get; }

    protected Documento(string valor)
    {
        Valor = Limpar(valor);
    }

    protected static string Limpar(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return string.Empty;
        return new string(valor.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
    }

    public abstract bool EhValido();

    public override string ToString() => Valor;

    public bool Equals(Documento? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Valor == other.Valor;
    }

    public override bool Equals(object? obj) => Equals(obj as Documento);

    public override int GetHashCode() => Valor.GetHashCode();

    public static bool operator ==(Documento? left, Documento? right) => Equals(left, right);

    public static bool operator !=(Documento? left, Documento? right) => !Equals(left, right);

    public static implicit operator string(Documento documento) => documento.Valor;
}
