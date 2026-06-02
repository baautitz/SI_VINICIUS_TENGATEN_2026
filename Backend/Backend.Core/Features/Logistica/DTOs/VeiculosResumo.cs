namespace Backend.Core.Features.Logistica.DTOs;

public record VeiculosResumo(
    int Id,
    string Placa,
    string EstadoSigla,
    string? MarcaModelo,
    string? TransportadoraNome,
    bool Ativo
);
