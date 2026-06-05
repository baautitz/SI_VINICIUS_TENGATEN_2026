using System.Text.Json.Serialization;

namespace Backend.Core.Features.Estoque.Entities.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StatusMovimentacaoEstoque
{
    RASCUNHO,
    CONFIRMADA,
    CANCELADA
}
