using Backend.Core.Common.Exceptions;

namespace Backend.Core.Features.Compras.Services;

// Rateio proporcional ao valor líquido do item (qtd * unit - desconto do item). Função pura.
public static class RateioCompra
{
    // despesasLiquidas = frete + seguro + outras + frete de terceiro - desconto geral.
    // Maior resto: cada item recebe o piso (em centavos) da sua parte exata; os centavos que sobram vao um a um
    // para os itens de maior resto (empate: maior valor, depois a ordem). A soma fecha exata e a diferenca entre itens iguais
    // nunca passa de 1 centavo.
    public static decimal[] Ratear(IReadOnlyList<decimal> valoresLiquidos, decimal despesasLiquidas)
    {
        var total = valoresLiquidos.Sum();
        if (total <= 0)
            throw new DomainException("O total dos produtos deve ser maior que zero para ratear as despesas.");

        var exatos = valoresLiquidos.Select(v => despesasLiquidas * 100m * v / total).ToArray();
        var pisos = exatos.Select(Math.Floor).ToArray();
        var sobra = (int)Math.Round(despesasLiquidas * 100m - pisos.Sum(), MidpointRounding.AwayFromZero);

        var receberao = Enumerable.Range(0, exatos.Length)
            .OrderByDescending(i => exatos[i] - pisos[i])
            .ThenByDescending(i => valoresLiquidos[i])
            .ThenBy(i => i)
            .Take(Math.Max(0, sobra))
            .ToHashSet();

        var rateios = pisos.Select((p, i) => (p + (receberao.Contains(i) ? 1 : 0)) / 100m).ToArray();

        // Despesa com fracao de centavo: o que ainda faltar fecha no maior item.
        var maior = Array.IndexOf(valoresLiquidos.ToArray(), valoresLiquidos.Max());
        rateios[maior] += despesasLiquidas - rateios.Sum();

        for (var i = 0; i < rateios.Length; i++)
            if (valoresLiquidos[i] + rateios[i] < 0)
                throw new DomainException("O desconto geral deixa o custo de um item negativo.");

        return rateios;
    }

    // 10 casas: com 4, 7000 un por 100,00 valorizava o estoque em 100,10. Erro residual < 0,005 até ~10 milhões de unidades.
    public const int CasasCusto = 10;

    public static decimal CustoUnitario(decimal quantidade, decimal valorLiquido, decimal rateio)
        => Math.Round((valorLiquido + rateio) / quantidade, CasasCusto, MidpointRounding.AwayFromZero);
}
