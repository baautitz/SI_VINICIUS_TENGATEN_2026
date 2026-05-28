using Backend.Core.Common;

namespace Backend.Core.Features.Catalogo.Entities;

public class SkusAtributosValores
{
    public string Sku { get; private set; }
    public int ChaveId { get; private set; }
    public string Valor { get; private set; }

    public Skus? SkuEntity { get; private set; }
    public SkuAtributosChaves? SkuAtributoChave { get; private set; }

    public SkusAtributosValores(string sku, int chaveId, string valor, Skus? skuEntity = null, SkuAtributosChaves? skuAtributoChave = null)
    {
        sku = TextNormalization.Normalize(sku);
        valor = TextNormalization.Normalize(valor);

        if (string.IsNullOrWhiteSpace(sku))
            throw new DomainException("SKU é obrigatório para atributo.");

        if (string.IsNullOrWhiteSpace(valor))
            throw new DomainException("Valor do atributo é obrigatório.");

        Sku = sku;
        ChaveId = chaveId;
        Valor = valor;
        SkuEntity = skuEntity;
        SkuAtributoChave = skuAtributoChave;
    }
}
