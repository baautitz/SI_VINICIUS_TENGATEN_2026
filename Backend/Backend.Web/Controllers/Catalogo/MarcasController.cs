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
[Route("api/catalogo/marcas")]
public class MarcasController : ControllerBase
{
    private readonly MarcasService _marcasService;

    public MarcasController(MarcasService marcasService)
    {
        _marcasService = marcasService;
    }

    [HttpGet]
    public Task<ResultadoPaginado<Marcas>> GetMarcas([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => _marcasService.ObterMarcas(search, page, pageSize);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Marcas>> GetMarca(int id)
    {
        var marca = await _marcasService.ObterMarcaPorId(id);
        if (marca is null)
            return NotFound();

        return Ok(marca);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Resultado<Marcas>), StatusCodes.Status201Created)]
    public async Task<ActionResult<Resultado<Marcas>>> CreateMarca([FromBody] CriarMarcaCommand command)
    {
        var result = await _marcasService.CriarMarca(command);
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetMarca), new { id = result.Data!.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Resultado<Marcas>>> UpdateMarca(int id, [FromBody] AtualizarMarcaCommand command)
    {
        var result = await _marcasService.AtualizarMarca(id, command);
        if (!result.Success)
        {
            if (result.Errors is not null && result.Errors.Any(error => error.Code == "MARCA_NAO_ENCONTRADA"))
                return NotFound(result);

            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteMarca(int id)
    {
        var deleted = await _marcasService.DeletarMarca(id);
        return deleted ? NoContent() : NotFound();
    }
}
