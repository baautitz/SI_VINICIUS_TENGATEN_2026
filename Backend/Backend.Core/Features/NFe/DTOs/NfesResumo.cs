namespace Backend.Core.Features.NFe.DTOs;

public record NfesResumo(
    int Id,
    string? ChaveAcesso,
    string TipoOperacao,
    int Numero,
    short Serie,
    DateTime DataEmissao,
    decimal ValorTotal,
    string StatusNfe
);
