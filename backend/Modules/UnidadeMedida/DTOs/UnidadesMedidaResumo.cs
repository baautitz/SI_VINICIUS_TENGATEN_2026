namespace backend.Modules.UnidadeMedida.DTOs;

public record UnidadesMedidaResumo(
    int Id,
    string Sigla,
    string Descricao,
    bool Ativo
);
