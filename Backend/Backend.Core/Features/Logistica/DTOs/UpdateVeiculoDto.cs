namespace Backend.Core.Features.Logistica.DTOs;

public record UpdateVeiculoDto(
    string Placa,
    int EstadoId,
    int? TransportadoraId,
    string? Rntrc,
    string? Renavam,
    string? TipoVeiculo,
    string? MarcaModelo,
    string? Observacao,
    bool Ativo
);
