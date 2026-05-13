namespace Modules.Pagamentos.Entities;

public class MetodosPagamentos
{
  public int Id { get; set; }
  public string Codigo { get; set; } = null!;
  public string Descricao { get; set; } = null!;
  public bool Ativo { get; set; }

  public ICollection<CondicoesPagamentos> CondicoesPagamentos { get; set; } = new List<CondicoesPagamentos>();
}