using System.Text.Json.Serialization;

namespace Backend.Core.Features.Estoque.Entities.Enums;

// Motivo tipado de toda saída manual e de toda divergência do balanço (ADR 0006).
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MotivoMovimentacaoEstoque
{
    PERDA_AVARIA,
    FURTO_ROUBO,
    VENCIMENTO,
    ERRO_DE_CONTAGEM,
    OUTROS
}

public static class MotivoMovimentacaoEstoqueExtensions
{
    // Perda, furto e vencimento tiram mercadoria do estoque e exigem NF-e de baixa; os demais são ajuste interno.
    public static bool GeraNfeDeBaixa(this MotivoMovimentacaoEstoque motivo)
        => motivo is MotivoMovimentacaoEstoque.PERDA_AVARIA
            or MotivoMovimentacaoEstoque.FURTO_ROUBO
            or MotivoMovimentacaoEstoque.VENCIMENTO;
}
