namespace Backend.Core.Features.Catalogo.DTOs;

public sealed class UpdateUnidadeMedidaDto
{
    public string Sigla { get; init; } = null!;
    public string Descricao { get; init; } = null!;
    public string Categoria { get; init; } = null!;
    public bool PermiteDecimais { get; init; } = false;
    public bool Ativo { get; init; } = true;
}
