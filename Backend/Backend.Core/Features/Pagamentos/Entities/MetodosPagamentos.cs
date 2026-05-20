namespace Backend.Core.Features.Pagamentos.Entities;

public class MetodosPagamentos
{
    public int Id { get; set; }
    public required string Codigo { get; set; }
    public required string Descricao { get; set; }
    public bool Ativo { get; set; }
}