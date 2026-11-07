using System.Text.Json.Serialization;

namespace Backend.Core.Features.Compras.Entities.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StatusDevolucaoCompra
{
    ATIVA,
    ESTORNADA
}
