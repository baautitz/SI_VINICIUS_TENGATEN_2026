using Backend.Core.Parceiros.Entities;

namespace Backend.Core.Vendas.Entities;

public class Venda
{
  public int Id { get; set; }
  public DateTime DataVenda { get; set; }
  public decimal ValorTotal { get; set; }
  public string? Observacao { get; set; }

  public required Emitentes Emitente { get; set; }
  public required Clientes Cliente { get; set; }
}