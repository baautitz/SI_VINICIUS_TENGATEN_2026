using Backend.Core.Features.NFe.Entities.Enums;
using Backend.Core.Features.Pagamentos.Entities;

namespace Backend.Core.Features.NFe.Entities;

public class NfesPagamentos
{
    public int Id { get; set; }
    public int NfeId { get; set; }
    public IndicadorPagamento IndicadorPagamento { get; set; }
    public decimal ValorPagamento { get; set; }

    public required MetodosPagamentos MetodosPagamento { get; set; }
}