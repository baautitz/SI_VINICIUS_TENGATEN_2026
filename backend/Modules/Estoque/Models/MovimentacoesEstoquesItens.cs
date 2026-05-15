using backend.Modules.Catalogo.Models;

namespace backend.Modules.Estoque.Models;

public class MovimentacoesEstoquesItens
{
  public int Id { get; set; }
  public decimal Quantidade { get; set; }
  public decimal CustoUnitario { get; set; }

  public required Skus Sku { get; set; }
}