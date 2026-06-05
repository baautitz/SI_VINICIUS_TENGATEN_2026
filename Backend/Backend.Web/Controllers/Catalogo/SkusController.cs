using Backend.Core.Common.Results;
using Backend.Core.Features.Catalogo.DTOs;
using Backend.Core.Features.Catalogo.Entities;
using Backend.Core.Features.Catalogo.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Backend.Web.Controllers.Catalogo;

[ApiController]
[Route("api/catalogo/skus")]
public class SkusController : ControllerBase
{
    private readonly ISkusRepository _skusRepository;

    public SkusController(ISkusRepository skusRepository)
    {
        _skusRepository = skusRepository;
    }

    [HttpGet]
    public Task<ResultadoPaginado<SkusResumo>> GetSkus([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        return _skusRepository.PesquisarSkus(search ?? string.Empty, page, pageSize);
    }

    [HttpGet("{sku}")]
    public async Task<ActionResult<Skus>> GetSkuBySku(string sku)
    {
        var result = await _skusRepository.ObterSkuPorSku(sku);
        if (result == null)
            return NotFound();

        return Ok(result);
    }
}
