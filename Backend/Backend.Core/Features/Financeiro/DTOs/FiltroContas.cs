using Backend.Core.Features.Financeiro.Entities.Enums;

namespace Backend.Core.Features.Financeiro.DTOs;

// Campos de filtro comuns a contas a pagar e a receber; um filtro novo comum entra aqui e em PesquisaTitulos.
public interface IFiltroTitulos
{
    int? ClienteId { get; }
    OrigemTituloFinanceiro? OrigemTipo { get; }
    int? OrigemId { get; }
    string? NumeroDocumento { get; }
    StatusTituloFinanceiro? Status { get; }
    string? CpfCnpj { get; }
    DateTime? VencimentoDe { get; }
    DateTime? VencimentoAte { get; }
    DateTime? EmissaoDe { get; }
    DateTime? EmissaoAte { get; }
    decimal? ValorMinimo { get; }
    decimal? ValorMaximo { get; }
}

public sealed record FiltroContasPagar(int? FornecedorId = null, int? ClienteId = null, OrigemTituloFinanceiro? OrigemTipo = null, int? OrigemId = null, string? NumeroDocumento = null, StatusTituloFinanceiro? Status = null,
    string? CpfCnpj = null, DateTime? VencimentoDe = null, DateTime? VencimentoAte = null, DateTime? EmissaoDe = null, DateTime? EmissaoAte = null, decimal? ValorMinimo = null, decimal? ValorMaximo = null, int? CompraId = null) : IFiltroTitulos;

public sealed record FiltroContasReceber(int? ClienteId = null, OrigemTituloFinanceiro? OrigemTipo = null, int? OrigemId = null, string? NumeroDocumento = null, StatusTituloFinanceiro? Status = null,
    string? CpfCnpj = null, DateTime? VencimentoDe = null, DateTime? VencimentoAte = null, DateTime? EmissaoDe = null, DateTime? EmissaoAte = null, decimal? ValorMinimo = null, decimal? ValorMaximo = null) : IFiltroTitulos;
