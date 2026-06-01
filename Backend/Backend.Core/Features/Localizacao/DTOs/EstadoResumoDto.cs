namespace Backend.Core.Features.Localizacao.DTOs;

public record EstadoResumoDto(
    int Id,
    string Estado,
    string Uf,
    int PaisId,
    string PaisNome
);