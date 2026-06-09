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
[Route("api/localizacao/paises")]
public class PaisesController : ControllerBase
{
    private readonly PaisesService _paisesService;

    public PaisesController(PaisesService paisesService)
    {
        _paisesService = paisesService;
    }

    [HttpGet]
    public Task<ResultadoPaginado<Paises>> GetPaises([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => _paisesService.ObterPaises(search, page, pageSize);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Paises>> GetPais(int id)
    {
        var pais = await _paisesService.ObterPaisPorId(id);
        if (pais is null)
            return NotFound();

        return Ok(pais);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Resultado<Paises>), StatusCodes.Status201Created)]
    public async Task<ActionResult<Resultado<Paises>>> CreatePais([FromBody] CriarPaisCommand command)
    {
        var result = await _paisesService.CriarPais(command);
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetPais), new { id = result.Data!.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Resultado<Paises>>> UpdatePais(int id, [FromBody] AtualizarPaisCommand command)
    {
        var result = await _paisesService.AtualizarPais(id, command);
        if (!result.Success)
        {
            if (result.Errors is not null && result.Errors.Any(error => error.Code == "PAIS_NAO_ENCONTRADO"))
                return NotFound(result);

            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeletePais(int id)
    {
        var deleted = await _paisesService.DeletarPais(id);
        return deleted ? NoContent() : NotFound();
    }
}
