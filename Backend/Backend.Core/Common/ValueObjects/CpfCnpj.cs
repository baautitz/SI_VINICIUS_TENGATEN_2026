namespace Backend.Core.Common.ValueObjects;

public class CpfCnpj : Documento
{
    public CpfCnpj(string valor) : base(valor) { }

    public override bool EhValido()
    {
        if (string.IsNullOrWhiteSpace(Valor)) return false;

        if (Valor.Length == 11)
            return new Cpf(Valor).EhValido();
        
        if (Valor.Length == 14)
            return new Cnpj(Valor).EhValido();

        return false;
    }

    public bool EhCpf() => Valor.Length == 11;
    public bool EhCnpj() => Valor.Length == 14;

    public static implicit operator CpfCnpj(string valor) => new(valor);
}
