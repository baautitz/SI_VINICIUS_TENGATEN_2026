namespace Backend.Core.Features.Localizacao.DTOs;

public sealed class UpdateEstadoDto
{
  public string Estado { get; init; } = null!;
  public string Uf { get; init; } = null!;
  public int PaisId { get; init; }
}
