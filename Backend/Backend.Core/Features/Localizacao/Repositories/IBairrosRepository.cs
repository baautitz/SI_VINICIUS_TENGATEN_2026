using Backend.Core.Common.Results;
using Backend.Core.Features.Localizacao.DTOs;
using Backend.Core.Features.Localizacao.Entities;

namespace Backend.Core.Features.Localizacao.Repositories;

public interface IBairrosRepository
{
    public Task<ResultadoPaginado<BairroResumoDto>> ObterBairros(string? search, int pagina = 1, int tamanhoDaPagina = 20);
    public Task<Bairros?> ObterBairroPorId(int id);
    public Task<Bairros> CriarBairro(Bairros cidade);
    public Task<Bairros> AtualizarBairro(int id, Bairros cidade);
    public Task<bool> DeletarBairro(int id);
    public Task<bool> ExisteBairro(int cidadeId, string bairro, int? ignorarId = null);
}

