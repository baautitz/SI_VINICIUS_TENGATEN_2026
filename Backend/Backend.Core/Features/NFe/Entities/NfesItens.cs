using Backend.Core.Features.Catalogo.Entities;
using Backend.Core.Features.UnidadeMedida.Entities;

namespace Backend.Core.Features.NFe.Entities;

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