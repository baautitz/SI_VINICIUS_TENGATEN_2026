using System.Text.Json.Serialization;

namespace Backend.Core.Features.Vendas.Entities.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StatusDevolucao
{
    ATIVA,
    ESTORNADA
}
