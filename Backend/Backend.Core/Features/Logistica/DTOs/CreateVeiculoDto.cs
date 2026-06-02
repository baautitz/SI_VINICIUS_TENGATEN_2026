namespace Backend.Core.Features.Logistica.DTOs;

public record CreateVeiculoDto(
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
