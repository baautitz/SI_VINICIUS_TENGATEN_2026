using Backend.Core.Common.ValueObjects;

namespace Backend.Core.Features.Logistica.DTOs;

public record TransportadorasResumo(
    int Id,
    string NomeRazaosocial,
    string CpfCnpj,
    bool Ativo
);
