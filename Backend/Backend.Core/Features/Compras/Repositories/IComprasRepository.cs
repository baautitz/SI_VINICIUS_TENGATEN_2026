using Backend.Core.Common.Results;
using Backend.Core.Features.Compras.Entities;

namespace Backend.Core.Features.Compras.Repositories;

public interface IComprasRepository
{
    public Task<ResultadoPaginado<Compra>> PesquisarCompras(string? termo, int pagina = 1, int tamanhoDaPagina = 20, int? fornecedorId = null);
    public Task<Compra?> ObterCompraPorId(int id);
    public Task<Compra> CriarCompra(Compra compra);

    // Vínculos fornecedor-SKU (cProd do fornecedor -> SKU interno).
    public Task<IReadOnlyDictionary<string, string>> ObterVinculosDoFornecedor(int fornecedorId);
    // Upsert: se (fornecedor, codigo) já existe, atualiza o SKU.
    public Task VincularSku(int fornecedorId, string codigoProduto, string sku);
}
