namespace Backend.Core.Logistica.DTOs;

public record TransportadorasResumo(
    int Id,
    string NomeRazaosocial,
    string CpfCnpj,
    bool Ativo
);
