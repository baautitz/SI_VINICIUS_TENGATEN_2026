namespace backend.Modules.NFe.DTOs;

public record NfesResumo(
    int Id,
    int Numero,
    short Serie,
    DateTime DataEmissao,
    decimal ValorTotal,
    string StatusNfe
);
