using Backend.Core.Common.Results;
using Backend.Core.Features.Compras.Commands;
using Backend.Core.Features.Compras.Entities;
using Backend.Core.Features.Compras.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Web.Controllers.Compras;

[ApiController]
public class ComprasController : ControllerBase
{
    private readonly ComprasService _comprasService;

    public ComprasController(ComprasService comprasService)
    {
        _comprasService = comprasService;
    }

    [HttpGet("api/compras")]
    public Task<ResultadoPaginado<Compra>> GetCompras([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] int? fornecedorId = null)
        => _comprasService.PesquisarCompras(search, page, pageSize, fornecedorId);

    [HttpGet("api/compras/{id:int}")]
    public async Task<ActionResult<Compra>> GetCompra(int id)
    {
        var compra = await _comprasService.ObterCompraPorId(id);
        return compra is null ? NotFound() : Ok(compra);
    }

    [HttpPost("api/compras")]
    [ProducesResponseType(typeof(Resultado<Compra>), StatusCodes.Status201Created)]
    public async Task<ActionResult<Resultado<Compra>>> CreateCompra([FromBody] CriarCompraCommand command)
    {
        var result = await _comprasService.CriarCompra(command);
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetCompra), new { id = result.Data!.Id }, result);
    }

    // codigoProduto (cProd do fornecedor) -> sku
    [HttpGet("api/fornecedores/{fornecedorId:int}/skus")]
    public Task<IReadOnlyDictionary<string, string>> GetVinculos(int fornecedorId)
        => _comprasService.ObterVinculosDoFornecedor(fornecedorId);

    public record VincularSkuRequest(string CodigoProduto, string Sku);

    [HttpPut("api/fornecedores/{fornecedorId:int}/skus")]
    public async Task<ActionResult<Resultado<string>>> VincularSku(int fornecedorId, [FromBody] VincularSkuRequest request)
    {
        var result = await _comprasService.VincularSku(fornecedorId, request.CodigoProduto, request.Sku);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
