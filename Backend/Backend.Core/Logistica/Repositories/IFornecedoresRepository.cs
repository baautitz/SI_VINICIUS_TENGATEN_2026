using Backend.Core.Logistica.DTOs;
using Backend.Core.Logistica.Entities;

namespace Backend.Core.Logistica.Repositories;

public interface IFornecedoresRepository
{
    public Task<IEnumerable<Fornecedores>> ObterFornecedores();
    public Task<Fornecedores?> ObterFornecedorPorId(int id);
    public Task<Fornecedores> CriarFornecedor(Fornecedores fornecedor);
    public Task<Fornecedores> AtualizarFornecedor(int id, Fornecedores fornecedor);
    public Task<bool> DeletarFornecedor(int id);
    public Task<IEnumerable<FornecedoresResumo>> ObterFornecedoresResumo();
    public Task<IEnumerable<FornecedoresResumo>> PesquisarFornecedores(string termo);
}
