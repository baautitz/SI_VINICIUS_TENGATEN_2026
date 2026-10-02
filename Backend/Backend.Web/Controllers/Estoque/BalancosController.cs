using Backend.Core.Common.Results;
using Backend.Core.Features.Estoque.Commands;
using Backend.Core.Features.Estoque.Entities;
using Backend.Core.Features.Estoque.Services;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace Backend.Web.Controllers.Estoque;

[ApiController]
[Route("api/estoque/balancos")]
public class BalancosController : ControllerBase
{
    private readonly BalancosService _balancosService;

    public BalancosController(BalancosService balancosService)
    {
        _balancosService = balancosService;
    }

    [HttpGet]
    public Task<ResultadoPaginado<Balancos>> GetBalancos([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => _balancosService.ObterBalancos(page, pageSize);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Balancos>> GetBalanco(int id)
    {
        var balanco = await _balancosService.ObterBalancoPorId(id);
        return balanco is null ? NotFound() : Ok(balanco);
    }

    [HttpPost]
    public async Task<ActionResult<Resultado<Balancos>>> CreateBalanco([FromBody] CriarBalancoCommand command)
    {
        var result = await _balancosService.CriarBalanco(command);
        return result.Success ? CreatedAtAction(nameof(GetBalanco), new { id = result.Data!.Id }, result) : BadRequest(result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Resultado<Balancos>>> UpdateBalanco(int id, [FromBody] AtualizarBalancoCommand command)
        => Responder(await _balancosService.AtualizarBalanco(id, command));

    [HttpPost("{id:int}/fechar")]
    public async Task<ActionResult<Resultado<Balancos>>> Fechar(int id)
        => Responder(await _balancosService.Fechar(id));

    [HttpPost("{id:int}/cancelar")]
    public async Task<ActionResult<Resultado<Balancos>>> Cancelar(int id, [FromBody] CancelarBalancoCommand? command)
        => Responder(await _balancosService.Cancelar(id, command));

    private ActionResult<Resultado<Balancos>> Responder(Resultado<Balancos> result)
    {
        if (result.Success) return Ok(result);
        if (result.Errors is not null && result.Errors.Any(e => e.Code == "BALANCO_INEXISTENTE")) return NotFound(result);
        return BadRequest(result);
    }
}
