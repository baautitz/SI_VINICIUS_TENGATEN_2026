using Backend.Core.Common.Results;
using Backend.Core.Features.Logistica.DTOs;
using Backend.Core.Features.Logistica.Entities;

namespace Backend.Core.Features.Logistica.Repositories;

public interface IVeiculosRepository
{
    public Task<ResultadoPaginado<Veiculos>> ObterVeiculos(int pagina = 1, int tamanhoDaPagina = 20);
    public Task<Veiculos?> ObterVeiculoPorId(int id);
    public Task<Veiculos> CriarVeiculo(Veiculos veiculo);
    public Task<Veiculos> AtualizarVeiculo(int id, Veiculos veiculo);
    public Task<bool> DeletarVeiculo(int id);
    public Task<ResultadoPaginado<VeiculosResumo>> ObterVeiculosResumo(int pagina = 1, int tamanhoDaPagina = 20);
    public Task<ResultadoPaginado<VeiculosResumo>> PesquisarVeiculos(string termo, int pagina = 1, int tamanhoDaPagina = 20);
}
