using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend.Core.Common.Results;
using Backend.Core.Features.Compras.Commands;
using Backend.Core.Features.Compras.Entities;
using Backend.Core.Features.Compras.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Web.Controllers.Compras;

[ApiController]
public class DevolucoesCompraController : ControllerBase
{
    private readonly DevolucoesCompraService _service;

    public DevolucoesCompraController(DevolucoesCompraService service)
    {
        _service = service;
    }

    [HttpGet("api/compras/{compraId:int}/devolucoes")]
    public Task<IReadOnlyList<DevolucaoCompra>> GetDevolucoesDaCompra(int compraId)
        => _service.ObterDevolucoesPorCompra(compraId);

    [HttpGet("api/devolucoes-compra/{id:int}")]
    public async Task<ActionResult<DevolucaoCompra>> GetDevolucao(int id)
    {
        var devolucao = await _service.ObterDevolucaoPorId(id);
        return devolucao is null ? NotFound() : Ok(devolucao);
    }

    [HttpPost("api/compras/{compraId:int}/devolucoes")]
    [ProducesResponseType(typeof(Resultado<DevolucaoCompra>), StatusCodes.Status201Created)]
    public async Task<ActionResult<Resultado<DevolucaoCompra>>> CreateDevolucao(int compraId, [FromBody] CriarDevolucaoCompraCommand command)
    {
        var result = await _service.CriarDevolucao(compraId, command);
        if (result.Success)
            return CreatedAtAction(nameof(GetDevolucao), new { id = result.Data!.Id }, result);
        if (result.Errors!.Any(e => e.Code == DevolucoesCompraService.CompraInexistente))
            return NotFound(result);
        return BadRequest(result);
    }
}
