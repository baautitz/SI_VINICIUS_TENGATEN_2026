namespace Backend.Core.Features.Pagamentos.DTOs;

public record CondicoesPagamentosResumo(
    int Id,
    string Descricao,
    bool Ativo
);
