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
[Route("api/catalogo/unidades-medida")]
public class UnidadesMedidaController : ControllerBase
{
    private readonly UnidadesMedidaService _unidadesMedidaService;

    public UnidadesMedidaController(UnidadesMedidaService unidadesMedidaService)
    {
        _unidadesMedidaService = unidadesMedidaService;
    }

    [HttpGet]
    public Task<ResultadoPaginado<UnidadesMedida>> GetUnidadesMedida([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => _unidadesMedidaService.ObterUnidadesMedida(search, page, pageSize);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UnidadesMedida>> GetUnidadeMedida(int id)
    {
        var um = await _unidadesMedidaService.ObterUnidadeMedidaPorId(id);
        if (um is null)
            return NotFound();

        return Ok(um);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Resultado<UnidadesMedida>), StatusCodes.Status201Created)]
    public async Task<ActionResult<Resultado<UnidadesMedida>>> CreateUnidadeMedida([FromBody] CriarUnidadeMedidaCommand command)
    {
        var result = await _unidadesMedidaService.CriarUnidadeMedida(command);
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetUnidadeMedida), new { id = result.Data!.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Resultado<UnidadesMedida>>> UpdateUnidadeMedida(int id, [FromBody] AtualizarUnidadeMedidaCommand command)
    {
        var result = await _unidadesMedidaService.AtualizarUnidadeMedida(id, command);
        if (!result.Success)
        {
            if (result.Errors is not null && result.Errors.Any(error => error.Code == "UNIDADE_MEDIDA_NAO_ENCONTRADA"))
                return NotFound(result);

            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteUnidadeMedida(int id)
    {
        var deleted = await _unidadesMedidaService.DeletarUnidadeMedida(id);
        return deleted ? NoContent() : NotFound();
    }
}
