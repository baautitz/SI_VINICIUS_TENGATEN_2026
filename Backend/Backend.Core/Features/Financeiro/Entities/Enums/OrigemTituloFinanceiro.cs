using System.Text.Json.Serialization;

namespace Backend.Core.Features.Financeiro.Entities.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrigemTituloFinanceiro
{
    MANUAL,
    VENDA,
    COMPRA,
    DEVOLUCAO_VENDA
}

public static class OrigemTituloFinanceiroExtensions
{
    public static string Nome(this OrigemTituloFinanceiro origem) => origem switch
    {
        OrigemTituloFinanceiro.VENDA => "Venda",
        OrigemTituloFinanceiro.COMPRA => "Compra",
        OrigemTituloFinanceiro.DEVOLUCAO_VENDA => "Devolução de venda",
        _ => "Manual"
    };
}
