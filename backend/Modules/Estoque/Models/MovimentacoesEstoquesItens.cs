namespace Modules.Estoque.Models;

public class MovimentacoesEstoquesItens
{
  public int Id { get; set; }
  public int MovimentacaoEstoqueId { get; set; }
  public int SkuId { get; set; }
  public decimal Quantidade { get; set; }
  public decimal CustoUnitario { get; set; }

  public required MovimentacoesEstoques MovimentacaoEstoque { get; set; }
  public required Catalogo.Models.Skus Sku { get; set; }
}