using Backend.Core.Common;
using Backend.Core.Features.Localizacao.DTOs;
using Backend.Core.Features.Localizacao.Entities;

namespace Backend.Core.Features.Localizacao.Repositories;

public interface ICidadesRepository
{
    public Task<ResultadoPaginado<Cidades>> ObterCidades(int pagina = 1, int tamanhoDaPagina = 20);
    public Task<Cidades?> ObterCidadePorId(int id);
    public Task<Cidades> CriarCidade(Cidades cidade);
    public Task<Cidades> AtualizarCidade(int id, Cidades cidade);
    public Task<bool> DeletarCidade(int id);
    public Task<ResultadoPaginado<CidadeResumoDto>> ObterCidadesResumo(int pagina = 1, int tamanhoDaPagina = 20);
    public Task<ResultadoPaginado<CidadeResumoDto>> PesquisarCidades(string termo, int pagina = 1, int tamanhoDaPagina = 20);
}

