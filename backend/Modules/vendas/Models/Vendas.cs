using backend.Modules.Parceiros.Models;

namespace backend.Modules.Vendas.Models;

public class Venda
{
  public int Id { get; set; }
  public DateTime DataVenda { get; set; }
  public decimal ValorTotal { get; set; }
  public string? Observacao { get; set; }

  public required Emitentes Emitente { get; set; }
  public required Clientes Cliente { get; set; }
}