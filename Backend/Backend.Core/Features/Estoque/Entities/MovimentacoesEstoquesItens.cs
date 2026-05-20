using Backend.Core.Features.Catalogo.Entities;

namespace Backend.Core.Features.Estoque.Entities;

public class MovimentacoesEstoquesItens
{
    public int Id { get; set; }
    public decimal Quantidade { get; set; }
    public decimal CustoUnitario { get; set; }

    public required Skus Sku { get; set; }
}