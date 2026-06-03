using Backend.Core.Common.Results;
using Backend.Core.Features.Catalogo.DTOs;
using Backend.Core.Features.Catalogo.Entities;
using Backend.Core.Features.Catalogo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Threading.Tasks;

namespace Backend.Web.Controllers.Catalogo;

[ApiController]
[Route("api/catalogo/atributos")]
public class SkuAtributosChavesController : ControllerBase
{
    private readonly SkuAtributosChavesService _atributosService;

    public SkuAtributosChavesController(SkuAtributosChavesService atributosService)
    {
        _atributosService = atributosService;
    }

    [HttpGet]
    public Task<ResultadoPaginado<SkuAtributosChavesResumo>> GetAtributos([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => _atributosService.ObterAtributos(search, page, pageSize);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SkuAtributosChaves>> GetAtributo(int id)
    {
        var atributo = await _atributosService.ObterAtributoPorId(id);
        if (atributo is null)
            return NotFound();

        return Ok(atributo);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Resultado<SkuAtributosChaves>), StatusCodes.Status201Created)]
    public async Task<ActionResult<Resultado<SkuAtributosChaves>>> CreateAtributo([FromBody] CreateSkuAtributosChavesDto dto)
    {
        var result = await _atributosService.CriarAtributo(dto);
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetAtributo), new { id = result.Data!.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Resultado<SkuAtributosChaves>>> UpdateAtributo(int id, [FromBody] UpdateSkuAtributosChavesDto dto)
    {
        var result = await _atributosService.AtualizarAtributo(id, dto);
        if (!result.Success)
        {
            if (result.Errors is not null && result.Errors.Any(error => error.Code == "ATRIBUTO_NAO_ENCONTRADO"))
                return NotFound(result);

            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAtributo(int id)
    {
        var deleted = await _atributosService.DeletarAtributo(id);
        return deleted ? NoContent() : NotFound();
    }
}
