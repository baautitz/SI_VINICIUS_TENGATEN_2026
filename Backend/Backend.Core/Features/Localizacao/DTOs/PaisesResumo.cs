namespace Backend.Core.Features.Localizacao.DTOs;

public record PaisesResumo(
    int Id,
    string Pais,
    string SiglaIso,
    string Ddi,
    string Moeda,
    string SimboloMoeda
);
