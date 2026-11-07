using Backend.Core.Features.Compras.Entities;

namespace Backend.Core.Features.Compras.Repositories;

// Devolução de compra é imutável: cria e consulta.
public interface IDevolucoesCompraRepository
{
    public Task<DevolucaoCompra?> ObterDevolucaoPorId(int id);
    public Task<IReadOnlyList<DevolucaoCompra>> ObterDevolucoesPorCompra(int compraId);
    public Task<DevolucaoCompra> CriarDevolucao(DevolucaoCompra devolucao);
    public Task CriarAbatimento(DevolucaoCompraAbatimento abatimento);
}
