namespace Backend.Core.Features.Relacionados;

public record RelacionadoItem(int Id, string Descricao, string? Detalhe);

/// <summary>Consultas leves "filhos de X" usadas pela navegação interligada do frontend.</summary>
public interface IRelacionadosRepository
{
    Task<IReadOnlyList<RelacionadoItem>> ContasReceberPorVenda(int vendaId);
    Task<IReadOnlyList<RelacionadoItem>> MovimentacoesPorVenda(int vendaId);
    Task<IReadOnlyList<RelacionadoItem>> ContasReceberPorCliente(int clienteId);
    Task<IReadOnlyList<RelacionadoItem>> ContasPagarPorFornecedor(int fornecedorId);
    Task<IReadOnlyList<RelacionadoItem>> VendasPorCliente(int clienteId);
    Task<IReadOnlyList<RelacionadoItem>> ProdutosPor(string campo, int id);
    Task<IReadOnlyList<RelacionadoItem>> LocalizacaoFilhos(string pai, int id);
}
