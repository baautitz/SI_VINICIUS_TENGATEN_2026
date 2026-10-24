using Backend.Core.Features.Financeiro.Entities.Enums;

namespace Backend.Core.Features.Financeiro.DTOs;

public sealed record FiltroContasPagar(int? FornecedorId = null, int? ClienteId = null, OrigemTituloFinanceiro? OrigemTipo = null, int? OrigemId = null, string? NumeroDocumento = null, StatusTituloFinanceiro? Status = null,
    string? CpfCnpj = null, DateTime? VencimentoDe = null, DateTime? VencimentoAte = null, DateTime? EmissaoDe = null, DateTime? EmissaoAte = null, decimal? ValorMinimo = null, decimal? ValorMaximo = null, int? CompraId = null);

public sealed record FiltroContasReceber(int? ClienteId = null, OrigemTituloFinanceiro? OrigemTipo = null, int? OrigemId = null, string? NumeroDocumento = null, StatusTituloFinanceiro? Status = null,
    string? CpfCnpj = null, DateTime? VencimentoDe = null, DateTime? VencimentoAte = null, DateTime? EmissaoDe = null, DateTime? EmissaoAte = null, decimal? ValorMinimo = null, decimal? ValorMaximo = null);
