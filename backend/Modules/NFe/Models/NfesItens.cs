using backend.Modules.Catalogo.Models;
using backend.Modules.UnidadeMedida.Models;

namespace backend.Modules.NFe.Models;

public class NfesItens
{
  public int Id { get; set; }
  public int NumeroItem { get; set; }
  public required string DescricaoItem { get; set; }
  public decimal Quantidade { get; set; }
  public decimal ValorUnitario { get; set; }
  public decimal ValorDesconto { get; set; }
  public decimal ValorTotal { get; set; }

  public required Skus Sku { get; set; }
  public required UnidadesMedida UnidadeMedida { get; set; }
}