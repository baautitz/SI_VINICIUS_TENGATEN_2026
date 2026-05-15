namespace backend.Modules.UnidadeMedida.Models;

public class UnidadesMedida
{
  public int Id { get; set; }
  public required string Sigla { get; set; }
  public required string Descricao { get; set; }
  public required string Categoria { get; set; }
  public bool Ativo { get; set; }
}