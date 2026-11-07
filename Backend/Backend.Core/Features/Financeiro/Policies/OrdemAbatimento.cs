namespace Backend.Core.Features.Financeiro.Policies;

// Ordem em que uma Devolução abate as parcelas de um título: primeiro as vencidas, da mais antiga para a mais
// recente (imputação a dívida vencida, Código Civil art. 355); depois as a vencer, da última para a primeira,
// o que preserva os vencimentos mais próximos. Vale para parcelas a receber (venda) e a pagar (compra).
public static class OrdemAbatimento
{
    public static IEnumerable<T> Ordenar<T>(IEnumerable<T> parcelas, Func<T, DateTime> vencimento, Func<T, int> numeroParcela, DateTime hoje)
    {
        var dia = hoje.Date;
        var lista = parcelas.ToList();

        var vencidas = lista.Where(p => vencimento(p).Date < dia)
            .OrderBy(p => vencimento(p).Date).ThenBy(numeroParcela);
        var aVencer = lista.Where(p => vencimento(p).Date >= dia)
            .OrderByDescending(numeroParcela);

        return vencidas.Concat(aVencer);
    }
}
