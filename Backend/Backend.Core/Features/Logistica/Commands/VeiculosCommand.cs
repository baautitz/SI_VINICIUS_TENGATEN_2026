namespace Backend.Core.Features.Logistica.Commands;

public record CriarVeiculoCommand(
    string Placa,
    int EstadoId,
    int? TransportadoraId = null,
    string? Rntrc = null,
    string? Renavam = null,
    string? TipoVeiculo = null,
    string? MarcaModelo = null,
    string? Observacao = null,
    bool Ativo = true
);

public record AtualizarVeiculoCommand(
    string Placa,
    int EstadoId,
    int? TransportadoraId = null,
    string? Rntrc = null,
    string? Renavam = null,
    string? TipoVeiculo = null,
    string? MarcaModelo = null,
    string? Observacao = null,
    bool Ativo = true
);
