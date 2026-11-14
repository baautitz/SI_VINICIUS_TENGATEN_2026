using System.Collections.Generic;

namespace Backend.Core.Features.Estoque.Commands;

// Lançamento manual: sempre saída, de origem MANUAL. O motivo tipado e o texto do motivo são obrigatórios;
// o emitente é obrigatório quando o motivo gera NF-e de baixa (perda/avaria, furto/roubo, vencimento).
public record CriarMovimentacaoCommand(
    string MotivoTipo,
    int? UsuarioId,
    string Motivo,
    int? EmitenteId,
    List<MovimentacaoItemCommand> Itens
);

public record MovimentacaoItemCommand(
    string Sku,
    decimal Quantidade,
    decimal? CustoUnitario
);
