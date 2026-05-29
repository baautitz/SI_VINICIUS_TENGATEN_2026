namespace Backend.Core.Features.Localizacao.DTOs;

public sealed class UpdateBairroDto
{
  public string Bairro { get; init; } = null!;
  public int CidadeId { get; init; }
}
