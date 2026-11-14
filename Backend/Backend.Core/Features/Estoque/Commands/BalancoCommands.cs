using System.Collections.Generic;

namespace Backend.Core.Features.Estoque.Commands;

// Motivo e recontagem valem só para o item que diverge do saldo: falta exige motivo; sobra exige recontagem e motivo.
public record ContagemItemCommand(string Sku, decimal QuantidadeContada, string? MotivoDivergencia = null, bool Recontado = false);

public record CriarBalancoCommand(int? UsuarioId, string? Observacao, List<ContagemItemCommand> Itens);

public record AtualizarBalancoCommand(string? Observacao, List<ContagemItemCommand> Itens);

// O emitente é obrigatório quando alguma falta gera NF-e de baixa (perda/avaria, furto/roubo, vencimento).
public record FecharBalancoCommand(int? EmitenteId);

// Motivo obrigatório (mínimo 5 caracteres) só ao cancelar um balanço já fechado.
public record CancelarBalancoCommand(string? Motivo);
