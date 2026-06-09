using Backend.Core.Common.Results;
using Backend.Core.Features.Catalogo.Commands;
using Backend.Core.Features.Catalogo.Entities;
using Backend.Core.Features.Catalogo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Threading.Tasks;

namespace Backend.Web.Controllers.Catalogo;

[ApiController]
[Route("api/catalogo/skus")]
public class SkusController : ControllerBase
{
    private readonly SkusService _skusService;

    public SkusController(SkusService skusService)
    {
        _skusService = skusService;
    }

    [HttpGet]
    public Task<ResultadoPaginado<Skus>> GetSkus([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => _skusService.ObterSkus(search, page, pageSize);

    [HttpGet("{sku}")]
    public async Task<ActionResult<Skus>> GetSku(string sku)
    {
        var skuResult = await _skusService.ObterSkuCompleto(sku);
        if (skuResult is null)
            return NotFound();

        return Ok(skuResult);
    }
}
