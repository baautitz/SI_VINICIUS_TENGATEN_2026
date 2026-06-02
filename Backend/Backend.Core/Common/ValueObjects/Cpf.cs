namespace Backend.Core.Common.ValueObjects;

public class Cpf : Documento
{
    public Cpf(string valor) : base(valor) { }

    public override bool EhValido()
    {
        if (string.IsNullOrWhiteSpace(Valor) || Valor.Length != 11) return false;

        if (new string(Valor[0], 11) == Valor) return false;

        var multiplicador1 = new[] { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
        var multiplicador2 = new[] { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

        var tempCpf = Valor.Substring(0, 9);
        var soma = 0;

        for (var i = 0; i < 9; i++)
            soma += (tempCpf[i] - '0') * multiplicador1[i];

        var resto = soma % 11;
        resto = resto < 2 ? 0 : 11 - resto;

        var digito = resto.ToString();
        tempCpf = tempCpf + digito;
        soma = 0;

        for (var i = 0; i < 10; i++)
            soma += (tempCpf[i] - '0') * multiplicador2[i];

        resto = soma % 11;
        resto = resto < 2 ? 0 : 11 - resto;
        digito = digito + resto.ToString();

        return Valor.EndsWith(digito);
    }

    public static implicit operator Cpf(string valor) => new(valor);
}
