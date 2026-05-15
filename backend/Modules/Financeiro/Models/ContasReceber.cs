using backend.Modules.Parceiros.Models;
using backend.Modules.NFe.Models;
using backend.Modules.Pagamentos.Models;

namespace backend.Modules.Financeiro.Models;

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

  public ICollection<ContasReceberParcelas> ContasReceberParcelas { get; set; } = new List<ContasReceberParcelas>();
}
