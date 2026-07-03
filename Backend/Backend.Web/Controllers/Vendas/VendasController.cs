using System;
using System.Linq;
using System.Threading.Tasks;
using Backend.Core.Common.Results;
using Backend.Core.Features.Vendas.Commands;
using Backend.Core.Features.Vendas.Entities;
using Backend.Core.Features.Vendas.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Web.Controllers.Vendas;

[ApiController]
[Route("api/vendas")]
public class VendasController : ControllerBase
{
    private readonly VendasService _vendasService;

    public VendasController(VendasService vendasService)
    {
        _vendasService = vendasService;
    }

    [HttpGet]
    public Task<ResultadoPaginado<Venda>> GetVendas([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => _vendasService.PesquisarVendas(search ?? string.Empty, page, pageSize);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Venda>> GetVenda(int id)
    {
        var venda = await _vendasService.ObterVendaPorId(id);
        if (venda is null)
            return NotFound();

        return Ok(venda);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Resultado<Venda>), StatusCodes.Status201Created)]
    public async Task<ActionResult<Resultado<Venda>>> CreateVenda([FromBody] CriarVendaCommand command)
    {
        var result = await _vendasService.CriarVenda(command);
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetVenda), new { id = result.Data!.Id }, result);
    }

    [HttpPost("{id:int}/cancelar")]
    public async Task<IActionResult> CancelarVenda(int id, [FromBody] CancelarVendaCommand command)
    {
        var canceled = await _vendasService.CancelarVenda(id, command);
        return canceled ? NoContent() : NotFound();
    }
}
