namespace Modules.Vendas.Models;

public class Vendas
{
  public int Id { get; set; }
  public int EmitenteId { get; set; }
  public int ClienteId { get; set; }
  public DateTime DataVenda { get; set; }
  public int MovimentacaoEstoqueId { get; set; }
  public int ContaReceberId { get; set; }
  public int NfeId { get; set; }
  public decimal ValorTotal { get; set; }
  public string? Observacao { get; set; }

  public NFe.Models.Nfes Nfe { get; set; } = null!;
  public Estoque.Models.MovimentacoesEstoques MovimentacaoEstoque { get; set; } = null!;
}