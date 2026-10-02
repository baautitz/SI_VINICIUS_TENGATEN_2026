using Backend.Core.Common.Exceptions;

namespace Backend.Core.Features.Compras.Services;

// Rateio proporcional ao valor líquido do item (qtd * unit - desconto do item). Função pura.
public static class RateioCompra
{
    // despesasLiquidas = frete + seguro + outras + frete de terceiro - desconto geral.
    // Cada rateio é arredondado a 2 casas; o resíduo vai para o maior item, então a soma fecha exata.
    public static decimal[] Ratear(IReadOnlyList<decimal> valoresLiquidos, decimal despesasLiquidas)
    {
        var total = valoresLiquidos.Sum();
        if (total <= 0)
            throw new DomainException("O total dos produtos deve ser maior que zero para ratear as despesas.");

        var rateios = valoresLiquidos
            .Select(v => Math.Round(despesasLiquidas * v / total, 2, MidpointRounding.AwayFromZero))
            .ToArray();

        var maior = Array.IndexOf(valoresLiquidos.ToArray(), valoresLiquidos.Max());
        rateios[maior] += despesasLiquidas - rateios.Sum();

        for (var i = 0; i < rateios.Length; i++)
            if (valoresLiquidos[i] + rateios[i] < 0)
                throw new DomainException("O desconto geral deixa o custo de um item negativo.");

        return rateios;
    }

    public static decimal CustoUnitario(decimal quantidade, decimal valorLiquido, decimal rateio)
        => Math.Round((valorLiquido + rateio) / quantidade, 4, MidpointRounding.AwayFromZero);
}
