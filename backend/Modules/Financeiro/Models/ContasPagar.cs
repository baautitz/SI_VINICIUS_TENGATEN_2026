namespace Modules.Financeiro.Models;

public class ContasPagar
{
  public int Id { get; set; }
  public int FornecedorId { get; set; }
  public int? NfeId { get; set; }
  public required string Descricao { get; set; }
  public DateTime? DataEmissao { get; set; }
  public DateTime? DataVencimento { get; set; }
  public decimal ValorOriginal { get; set; }
  public decimal ValorSaldo { get; set; }
  public StatusTituloFinanceiro Status { get; set; }
  public int? CondicaoPagamentoId { get; set; }
  public string? Observacao { get; set; }
  public DateTime CriadoEm { get; set; }
  public DateTime? AtualizadoEm { get; set; }

  public required Logistica.Models.Fornecedores Fornecedor { get; set; }
  public NFe.Models.Nfes? Nfe { get; set; }
  public Pagamentos.Models.CondicoesPagamentos? CondicaoPagamento { get; set; }

  public ICollection<ContasPagarParcelas> ContasPagarParcelas { get; set; } = new List<ContasPagarParcelas>();
}