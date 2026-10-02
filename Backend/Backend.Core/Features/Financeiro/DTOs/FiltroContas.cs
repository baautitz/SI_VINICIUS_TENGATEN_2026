using Backend.Core.Features.Financeiro.Entities.Enums;

namespace Backend.Core.Features.Financeiro.DTOs;

public sealed record FiltroContasPagar(int? FornecedorId = null, int? ClienteId = null, OrigemTituloFinanceiro? OrigemTipo = null, int? OrigemId = null);

public sealed record FiltroContasReceber(int? ClienteId = null, OrigemTituloFinanceiro? OrigemTipo = null, int? OrigemId = null);
