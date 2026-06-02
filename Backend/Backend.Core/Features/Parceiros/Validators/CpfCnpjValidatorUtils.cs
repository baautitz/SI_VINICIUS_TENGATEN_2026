using Backend.Core.Common.ValueObjects;

namespace Backend.Core.Features.Parceiros.Validators;

public static class CpfCnpjValidatorUtils
{
    public static bool IsValid(string cpfCnpj)
    {
        if (string.IsNullOrWhiteSpace(cpfCnpj)) return false;

        var value = new string(cpfCnpj.Where(char.IsLetterOrDigit).ToArray()).ToUpper();

        if (value.Length == 11 && value.All(char.IsDigit)) return IsCpf(value);
        if (value.Length == 14) return IsCnpj(value);

        return false;
    }

    private static bool IsCpf(string cpf)
    {
        int[] multiplicador1 = { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
        int[] multiplicador2 = { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

        string tempCpf;
        string digito;
        int soma;
        int resto;

        if (cpf.All(c => c == cpf[0])) return false;

        tempCpf = cpf.Substring(0, 9);
        soma = 0;

        for (int i = 0; i < 9; i++)
            soma += int.Parse(tempCpf[i].ToString()) * multiplicador1[i];

        resto = soma % 11;
        resto = (resto < 2) ? 0 : 11 - resto;

        digito = resto.ToString();
        tempCpf = tempCpf + digito;
        soma = 0;

        for (int i = 0; i < 10; i++)
            soma += int.Parse(tempCpf[i].ToString()) * multiplicador2[i];

        resto = soma % 11;
        resto = (resto < 2) ? 0 : 11 - resto;

        digito = digito + resto.ToString();

        return cpf.EndsWith(digito);
    }

    private static bool IsCnpj(string cnpj)
    {
        int[] multiplicador1 = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        int[] multiplicador2 = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

        int soma;
        int resto;
        string digito;
        string tempCnpj;

        if (cnpj.All(c => c == cnpj[0])) return false;
        
        if (!char.IsDigit(cnpj[12]) || !char.IsDigit(cnpj[13])) return false;

        tempCnpj = cnpj.Substring(0, 12);
        soma = 0;
        for (int i = 0; i < 12; i++)
            soma += (tempCnpj[i] - 48) * multiplicador1[i];

        resto = (soma % 11);
        resto = (resto < 2) ? 0 : 11 - resto;

        digito = resto.ToString();
        tempCnpj = tempCnpj + digito;
        soma = 0;

        for (int i = 0; i < 13; i++)
            soma += (tempCnpj[i] - 48) * multiplicador2[i];

        resto = (soma % 11);
        resto = (resto < 2) ? 0 : 11 - resto;

        digito = digito + resto.ToString();

        return cnpj.EndsWith(digito);
    }
}
