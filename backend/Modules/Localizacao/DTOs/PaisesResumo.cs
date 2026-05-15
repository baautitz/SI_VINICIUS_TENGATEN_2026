namespace backend.Modules.Localizacao.DTOs;

public record PaisesResumo(
    int Id,
    string Pais,
    string SiglaIso,
    string Ddi,
    string Moeda,
    string SimboloMoeda
);
