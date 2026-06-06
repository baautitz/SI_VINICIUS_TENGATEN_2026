namespace Backend.Core.Features.Catalogo.DTOs;

public record UnidadesMedidaResumo(
    int Id,
    string Sigla,
    string Descricao,
    string Categoria,
    bool PermiteDecimais,
    bool Ativo
);
