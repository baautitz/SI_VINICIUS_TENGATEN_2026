namespace Backend.Core.Features.Localizacao.DTOs;

public record PaisResumoDto(
    int Id,
    string Pais,
    string SiglaIso,
    string Ddi,
    string Moeda,
    string SimboloMoeda
);

