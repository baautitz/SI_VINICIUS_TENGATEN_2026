using Backend.Core.Features.Compras.Entities;

namespace Backend.Core.Features.Compras.Repositories;

// Devolução de compra é imutável: cria, consulta e marca como estornada.
public interface IDevolucoesCompraRepository
{
    public Task<DevolucaoCompra?> ObterDevolucaoPorId(int id);
    public Task<IReadOnlyList<DevolucaoCompra>> ObterDevolucoesPorCompra(int compraId);
    public Task<DevolucaoCompra> CriarDevolucao(DevolucaoCompra devolucao);
    // UPDATE guardado (status ATIVA): false se outro estorno já a marcou.
    public Task<bool> MarcarEstornada(DevolucaoCompra devolucao);
    public Task CriarAbatimento(DevolucaoCompraAbatimento abatimento);
    // Só os abatimentos ainda não estornados, na ordem em que foram gravados.
    public Task<IReadOnlyList<DevolucaoCompraAbatimento>> ObterAbatimentosAtivos(int devolucaoId);
    public Task MarcarAbatimentosEstornados(int devolucaoId, DateTime estornadoEm);
}
