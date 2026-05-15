namespace backend.Modules.Pagamentos.Models;

public class MetodosPagamentos
{
  public int Id { get; set; }
  public required string Codigo { get; set; }
  public required string Descricao { get; set; }
  public bool Ativo { get; set; }
}