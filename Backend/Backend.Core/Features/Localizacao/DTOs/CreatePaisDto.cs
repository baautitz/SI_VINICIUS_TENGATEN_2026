namespace Backend.Core.Features.Localizacao.DTOs;

public sealed class CreatePaisDto
{
  public string Ddi { get; init; } = null!;
  public string SiglaIso { get; init; } = null!;
  public string Moeda { get; init; } = null!;
  public string SimboloMoeda { get; init; } = null!;
  public string Pais { get; init; } = null!;
}
