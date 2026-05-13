namespace Modules.Financeiro.Entities;

public class ContasPagar
{
  public int Id { get; set; }
  public int FornecedorId { get; set; }
  public int? NfeId { get; set; }
  public string Descricao { get; set; } = null!;
  public DateTime? DataEmissao { get; set; }
  public DateTime? DataVencimento { get; set; }
  public decimal ValorOriginal { get; set; }
  public decimal ValorSaldo { get; set; }
  public StatusTituloFinanceiro Status { get; set; }
  public int? CondicaoPagamentoId { get; set; }
  public string? Observacao { get; set; }
  public DateTime CriadoEm { get; set; }
  public DateTime? AtualizadoEm { get; set; }

  public Logistica.Entities.Fornecedores Fornecedor { get; set; } = null!;
  public NFe.Entities.Nfes? Nfe { get; set; }
  public Pagamentos.Entities.CondicoesPagamentos? CondicaoPagamento { get; set; }

  public ICollection<ContasPagarParcelas> ContasPagarParcelas { get; set; } = new List<ContasPagarParcelas>();
}