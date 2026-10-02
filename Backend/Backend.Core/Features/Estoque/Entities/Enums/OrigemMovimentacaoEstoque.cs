using System.Text.Json.Serialization;

namespace Backend.Core.Features.Estoque.Entities.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrigemMovimentacaoEstoque
{
    MANUAL,
    VENDA,
    COMPRA,
    BALANCO,
    ESTORNO,
    DEVOLUCAO_VENDA
}
