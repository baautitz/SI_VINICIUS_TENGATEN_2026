namespace Modules.Estoque.Entities;

public class MovimentacoesEstoquesItens
{
  public int Id { get; set; }
  public int MovimentacaoEstoqueId { get; set; }
  public int SkuId { get; set; }
  public decimal Quantidade { get; set; }
  public decimal CustoUnitario { get; set; }

  public MovimentacoesEstoques MovimentacaoEstoque { get; set; } = null!;
  public Catalogo.Entities.Skus Sku { get; set; } = null!;
}