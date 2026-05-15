using backend.Modules.Parceiros.Models;
using backend.Modules.Financeiro.Models;
using backend.Modules.NFe.Models;
using backend.Modules.Estoque.Models;

namespace backend.Modules.Vendas.Models;

public class Vendas
{
  public int Id { get; set; }
  public DateTime DataVenda { get; set; }
  public decimal ValorTotal { get; set; }
  public string? Observacao { get; set; }

  public required Emitentes Emitente { get; set; }
  public required Clientes Cliente { get; set; }
  public required ContasReceber ContaReceber { get; set; }
  public required Nfes Nfe { get; set; }
  public required MovimentacoesEstoques MovimentacaoEstoque { get; set; }
}