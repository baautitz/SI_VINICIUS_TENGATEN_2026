using Backend.Core.Common.ValueObjects;

namespace Backend.Core.Features.Localizacao.DTOs;

public sealed class UpdateCidadeDto
{
  public string Cidade { get; init; } = null!;
  public short Ddd { get; init; }
  public int EstadoId { get; init; }
}
