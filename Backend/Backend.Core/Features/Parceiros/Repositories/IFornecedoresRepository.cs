using Backend.Core.Common;

using Backend.Core.Features.Parceiros.DTOs;

using Backend.Core.Features.Parceiros.Entities;

namespace Backend.Core.Features.Parceiros.Repositories;

public interface IFornecedoresRepository
{
    public Task<ResultadoPaginado<Fornecedores>> ObterFornecedores(int pagina = 1, int tamanhoDaPagina = 20);
    public Task<Fornecedores?> ObterFornecedorPorId(int id);
    public Task<Fornecedores> CriarFornecedor(Fornecedores fornecedor);
    public Task<Fornecedores> AtualizarFornecedor(int id, Fornecedores fornecedor);
    public Task<bool> DeletarFornecedor(int id);
    public Task<ResultadoPaginado<FornecedoresResumo>> ObterFornecedoresResumo(int pagina = 1, int tamanhoDaPagina = 20);
    public Task<ResultadoPaginado<FornecedoresResumo>> PesquisarFornecedores(string termo, int pagina = 1, int tamanhoDaPagina = 20);
}
