using System.Text.Json.Serialization;

namespace Backend.Core.Features.Financeiro.Entities.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrigemTituloFinanceiro
{
    MANUAL,
    VENDA,
    COMPRA,
    DEVOLUCAO_VENDA,
    FRETE_COMPRA,
    DEVOLUCAO_COMPRA
}

public static class OrigemTituloFinanceiroExtensions
{
    public static string Nome(this OrigemTituloFinanceiro origem) => origem switch
    {
        OrigemTituloFinanceiro.VENDA => "Venda",
        OrigemTituloFinanceiro.COMPRA => "Compra",
        OrigemTituloFinanceiro.DEVOLUCAO_VENDA => "Devolução de venda",
        OrigemTituloFinanceiro.FRETE_COMPRA => "Frete da compra",
        OrigemTituloFinanceiro.DEVOLUCAO_COMPRA => "Devolução de compra",
        _ => "Manual"
    };
}
