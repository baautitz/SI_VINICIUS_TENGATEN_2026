using Backend.Core.Catalogo.Entities;

namespace Backend.Core.Estoque.Entities;

public class MovimentacoesEstoquesItens
{
  public int Id { get; set; }
  public decimal Quantidade { get; set; }
  public decimal CustoUnitario { get; set; }

  public required Skus Sku { get; set; }
}