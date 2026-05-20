using Backend.Core.Features.Catalogo.Entities;

namespace Backend.Core.Features.Vendas.Entities;

public class VendaItens
{
    public int Id { get; set; }
    public int VendaId { get; set; }
    public decimal Quantidade { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal ValorDesconto { get; set; }
    public decimal ValorTotal { get; set; }

    public required Skus Sku { get; set; }
}
