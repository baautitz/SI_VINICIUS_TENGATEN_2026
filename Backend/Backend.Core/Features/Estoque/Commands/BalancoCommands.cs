using System.Collections.Generic;

namespace Backend.Core.Features.Estoque.Commands;

public record ContagemItemCommand(string Sku, decimal QuantidadeContada);

public record CriarBalancoCommand(int? UsuarioId, string? Observacao, List<ContagemItemCommand> Itens);

public record AtualizarBalancoCommand(string? Observacao, List<ContagemItemCommand> Itens);

// Motivo obrigatório (mínimo 5 caracteres) só ao cancelar um balanço já fechado.
public record CancelarBalancoCommand(string? Motivo);
