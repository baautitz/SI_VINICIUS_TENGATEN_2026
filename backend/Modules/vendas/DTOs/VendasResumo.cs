namespace backend.Modules.Vendas.DTOs;

public record VendasResumo(
    int Id,
    DateTime DataVenda,
    string ClienteNome,
    decimal ValorTotal
);
