using Backend.Core.Financeiro.Entities.Enums;
using Backend.Core.NFe.Entities;
using Backend.Core.Pagamentos.Entities;
using Backend.Core.Parceiros.Entities;

namespace Backend.Core.Financeiro.Entities;

public class ContasReceber
{
  public int Id { get; set; }
  public required string Descricao { get; set; }
  public DateTime? DataEmissao { get; set; }
  public DateTime? DataVencimento { get; set; }
  public decimal ValorOriginal { get; set; }
  public decimal ValorSaldo { get; set; }
  public StatusTituloFinanceiro Status { get; set; }
  public string? Observacao { get; set; }
  public DateTime CriadoEm { get; set; }
  public DateTime? AtualizadoEm { get; set; }

  public required Clientes Cliente { get; set; }
  public Nfes? Nfe { get; set; }
  public CondicoesPagamentos? CondicaoPagamento { get; set; }

  public required IEnumerable<ContasReceberParcelas> ContasReceberParcelas { get; set; }
}
