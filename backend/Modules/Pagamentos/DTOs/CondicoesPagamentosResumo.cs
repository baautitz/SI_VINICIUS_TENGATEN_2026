namespace backend.Modules.Pagamentos.DTOs;

public record CondicoesPagamentosResumo(
    int Id,
    string Descricao,
    bool Ativo
);
