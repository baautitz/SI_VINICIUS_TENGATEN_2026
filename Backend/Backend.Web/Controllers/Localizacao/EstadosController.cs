using Backend.Core.Common.Results;
using Backend.Core.Features.Localizacao.Commands;
using Backend.Core.Features.Localizacao.Entities;
using Backend.Core.Features.Localizacao.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Threading.Tasks;

namespace Backend.Web.Controllers.Localizacao;

[ApiController]
[Route("api/localizacao/estados")]
public class EstadosController : ControllerBase
{
    private readonly EstadosService _estadosService;

    public EstadosController(EstadosService estadosService)
    {
        _estadosService = estadosService;
    }

    [HttpGet]
    public Task<ResultadoPaginado<Estados>> GetEstados([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => _estadosService.ObterEstados(search, page, pageSize);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Estados>> GetEstado(int id)
    {
        var estado = await _estadosService.ObterEstadoPorId(id);
        if (estado is null)
            return NotFound();

        return Ok(estado);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Resultado<Estados>), StatusCodes.Status201Created)]
    public async Task<ActionResult<Resultado<Estados>>> CreateEstado([FromBody] CriarEstadoCommand command)
    {
        var result = await _estadosService.CriarEstado(command);
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetEstado), new { id = result.Data!.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Resultado<Estados>>> UpdateEstado(int id, [FromBody] AtualizarEstadoCommand command)
    {
        var result = await _estadosService.AtualizarEstado(id, command);
        if (!result.Success)
        {
            if (result.Errors is not null && result.Errors.Any(error => error.Code == "ESTADO_NAO_ENCONTRADO"))
                return NotFound(result);

            if (result.Errors is not null && result.Errors.Any(error => error.Code == "PAIS_NAO_ENCONTRADO"))
                return BadRequest(result);

            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteEstado(int id)
    {
        var deleted = await _estadosService.DeletarEstado(id);
        return deleted ? NoContent() : NotFound();
    }
}
