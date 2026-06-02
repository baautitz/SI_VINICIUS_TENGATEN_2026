namespace Backend.Core.Common.ValueObjects;

public readonly record struct Ddd
{
    public string Valor { get; }

    public Ddd(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            Valor = string.Empty;
            return;
        }

        Valor = new string(valor.Where(char.IsDigit).ToArray());
    }

    public Ddd(short valor)
    {
        Valor = valor.ToString();
    }

    public short ToShort() => short.TryParse(Valor, out var result) ? result : (short)0;

    public override string ToString() => Valor;

    public static implicit operator string(Ddd ddd) => ddd.Valor;
    public static implicit operator Ddd(string valor) => new(valor);
    public static implicit operator short(Ddd ddd) => ddd.ToShort();
    public static implicit operator Ddd(short valor) => new(valor);
}
