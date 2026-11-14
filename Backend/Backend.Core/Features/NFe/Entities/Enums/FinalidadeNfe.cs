namespace Backend.Core.Features.NFe.Entities.Enums;

public enum FinalidadeNfe
{
    NORMAL,
    DEVOLUCAO,
    ESTORNO_DEVOLUCAO,
    BAIXA_ESTOQUE,
    ESTORNO_BAIXA_ESTOQUE,
    ENTRADA_AJUSTE_INVENTARIO,
    ESTORNO_ENTRADA_AJUSTE_INVENTARIO
}

public static class FinalidadeNfeExtensions
{
    // Notas emitidas pela loja para si mesma a partir de uma Movimentação de estoque (ADR 0006).
    public static bool EhDeAjusteDeEstoque(this FinalidadeNfe finalidade)
        => finalidade is FinalidadeNfe.BAIXA_ESTOQUE or FinalidadeNfe.ESTORNO_BAIXA_ESTOQUE
            or FinalidadeNfe.ENTRADA_AJUSTE_INVENTARIO or FinalidadeNfe.ESTORNO_ENTRADA_AJUSTE_INVENTARIO;
}
