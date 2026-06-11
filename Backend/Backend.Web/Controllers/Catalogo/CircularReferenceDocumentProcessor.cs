using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;
using NJsonSchema;

namespace Backend.Web.Controllers.Catalogo;

public class CircularReferenceDocumentProcessor : IDocumentProcessor
{
    public void Process(DocumentProcessorContext context)
    {
        var schemas = context.Document.Components.Schemas;

        if (!schemas.ContainsKey("Produtos") || !schemas.ContainsKey("Skus"))
            return;

        var produtosSchema = schemas["Produtos"];
        var skusSchema = schemas["Skus"];

        var produtosWithoutSkus = CloneSchema(produtosSchema);
        produtosWithoutSkus.Properties.Remove("skus");
        schemas["ProdutosWithoutSkus"] = produtosWithoutSkus;

        var skusWithoutProduto = CloneSchema(skusSchema);
        skusWithoutProduto.Properties.Remove("produto");
        schemas["SkusWithoutProduto"] = skusWithoutProduto;

        if (produtosSchema.Properties.TryGetValue("skus", out var skusProp))
        {
            var item = skusProp.ActualSchema.Item;
            if (item != null)
            {
                item.Reference = skusWithoutProduto;
            }
            else if (skusProp.Item != null)
            {
                skusProp.Item.Reference = skusWithoutProduto;
            }
        }

        if (skusSchema.Properties.TryGetValue("produto", out var produtoProp))
        {
            produtoProp.Reference = produtosWithoutSkus;
            foreach (var oneOfSchema in produtoProp.OneOf)
            {
                if (oneOfSchema.Reference == produtosSchema)
                {
                    oneOfSchema.Reference = produtosWithoutSkus;
                }
            }
        }
    }

    private JsonSchema CloneSchema(JsonSchema source)
    {
        var clone = new JsonSchema
        {
            Type = source.Type,
            Format = source.Format,
            Description = source.Description,
            AllowAdditionalProperties = source.AllowAdditionalProperties,
            AdditionalPropertiesSchema = source.AdditionalPropertiesSchema
        };

        foreach (var req in source.RequiredProperties)
        {
            clone.RequiredProperties.Add(req);
        }

        foreach (var prop in source.Properties)
        {
            clone.Properties.Add(prop.Key, prop.Value);
        }

        return clone;
    }
}
