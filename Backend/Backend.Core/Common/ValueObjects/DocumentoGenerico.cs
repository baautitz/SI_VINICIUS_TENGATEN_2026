namespace Backend.Core.Common.ValueObjects;

public class DocumentoGenerico : Documento
{
    public DocumentoGenerico(string valor) : base(valor) { }

    public override bool EhValido() => !string.IsNullOrWhiteSpace(Valor);

    public static implicit operator DocumentoGenerico(string valor) => new(valor);
}
