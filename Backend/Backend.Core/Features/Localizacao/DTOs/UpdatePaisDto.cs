using Backend.Core.Common.ValueObjects;

namespace Backend.Core.Features.Localizacao.DTOs;

public sealed class UpdatePaisDto
{
  public string Ddi { get; init; } = null!;
  public string SiglaIso { get; init; } = null!;
  public string Moeda { get; init; } = null!;
  public string SimboloMoeda { get; init; } = null!;
  public string Pais { get; init; } = null!;
}
