using Backend.Core.Common.Exceptions;
using Backend.Core.Common.Helpers;

namespace Backend.Core.Features.Catalogo.Entities;

public class SkuAtributosValores
{
    public int Id { get; set; }
    public int ChaveId { get; private set; }
    public string Valor { get; private set; } = null!;

    protected SkuAtributosValores() { }

    public SkuAtributosValores(int chaveId, string valor)
    {
        valor = TextNormalization.Normalize(valor);
        if (string.IsNullOrWhiteSpace(valor))
            throw new DomainException("Valor do atributo é obrigatório.");

        ChaveId = chaveId;
        Valor = valor;
    }

    public SkuAtributosValores(int id, int chaveId, string valor)
        : this(chaveId, valor)
    {
        Id = id;
    }
}
