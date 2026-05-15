namespace backend.Modules.Catalogo.DTOs;

public record ProdutosResumo(
    int Id,
    string Produto,
    string Descricao,
    bool Ativo
);
