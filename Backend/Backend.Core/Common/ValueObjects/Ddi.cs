namespace Backend.Core.Common.ValueObjects;

public readonly record struct Ddi
{
    public string Valor { get; }

    public Ddi(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            Valor = string.Empty;
            return;
        }

        var digits = new string(valor.Where(char.IsDigit).ToArray());
        Valor = string.IsNullOrEmpty(digits) ? string.Empty : "+" + digits;
    }

    public override string ToString() => Valor;

    public static implicit operator string(Ddi ddi) => ddi.Valor;
    public static implicit operator Ddi(string valor) => new(valor);
}
