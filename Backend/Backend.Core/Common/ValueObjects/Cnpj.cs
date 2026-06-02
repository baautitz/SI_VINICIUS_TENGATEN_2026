namespace Backend.Core.Common.ValueObjects;

public class Cnpj : Documento
{
    public Cnpj(string valor) : base(valor) { }

    public override bool EhValido()
    {
        if (string.IsNullOrWhiteSpace(Valor) || Valor.Length != 14) return false;

        var multiplicador1 = new[] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        var multiplicador2 = new[] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

        var tempCnpj = Valor.Substring(0, 12);
        var soma = 0;

        for (var i = 0; i < 12; i++)
            soma += (tempCnpj[i] - '0') * multiplicador1[i];

        var resto = (soma % 11);
        resto = (resto < 2) ? 0 : 11 - resto;

        var digito = resto.ToString();
        tempCnpj = tempCnpj + digito;
        soma = 0;

        for (var i = 0; i < 13; i++)
            soma += (tempCnpj[i] - '0') * multiplicador2[i];

        resto = (soma % 11);
        resto = (resto < 2) ? 0 : 11 - resto;
        digito = digito + resto.ToString();

        return Valor.EndsWith(digito);
    }

    public static implicit operator Cnpj(string valor) => new(valor);
}
