namespace Backend.Core.Features.NFe.DTOs;

// NF-e de saída emitida e ainda ativa para uma venda.
public record NfeAtiva(int NfeId, string ChaveAcesso);

public record CancelarNfeCommand(string Justificativa);
