using System.Threading.Tasks;
using Backend.Core.Common.Results;
using Backend.Core.Features.Catalogo.Entities;
using Backend.Core.Features.Catalogo.Repositories;
using Backend.Core.Common;

namespace Backend.Core.Features.Catalogo.Services;

public sealed class SkusService : BaseService
{
    private readonly ISkusRepository _skusRepository;

    public SkusService(ISkusRepository skusRepository)
    {
        _skusRepository = skusRepository;
    }

    public Task<ResultadoPaginado<Skus>> ObterSkus(string? search, int pagina = 1, int tamanhoPagina = 20)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return _skusRepository.ObterSkus(pagina, tamanhoPagina);
        }
        return _skusRepository.PesquisarSkus(search, pagina, tamanhoPagina);
    }

    public async Task<Skus?> ObterSkuCompleto(string sku)
    {
        return await _skusRepository.ObterSkuCompleto(sku);
    }
}
