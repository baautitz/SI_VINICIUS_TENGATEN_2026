namespace Backend.Core.Features.Pagamentos.DTOs;

public record MetodosPagamentosResumo(
    int Id,
    string Codigo,
    string Descricao,
    bool Ativo
);
