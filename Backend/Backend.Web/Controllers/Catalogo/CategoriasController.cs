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
[Route("api/catalogo/categorias")]
public class CategoriasController : ControllerBase
{
    private readonly CategoriasService _categoriasService;

    public CategoriasController(CategoriasService categoriasService)
    {
        _categoriasService = categoriasService;
    }

    [HttpGet]
    public Task<ResultadoPaginado<CategoriasResumo>> GetCategorias([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => _categoriasService.ObterCategorias(search, page, pageSize);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Categorias>> GetCategoria(int id)
    {
        var categoria = await _categoriasService.ObterCategoriaPorId(id);
        if (categoria is null)
            return NotFound();

        return Ok(categoria);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Resultado<Categorias>), StatusCodes.Status201Created)]
    public async Task<ActionResult<Resultado<Categorias>>> CreateCategoria([FromBody] CreateCategoriaDto dto)
    {
        var result = await _categoriasService.CriarCategoria(dto);
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetCategoria), new { id = result.Data!.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Resultado<Categorias>>> UpdateCategoria(int id, [FromBody] UpdateCategoriaDto dto)
    {
        var result = await _categoriasService.AtualizarCategoria(id, dto);
        if (!result.Success)
        {
            if (result.Errors is not null && result.Errors.Any(error => error.Code == "CATEGORIA_NAO_ENCONTRADA"))
                return NotFound(result);

            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCategoria(int id)
    {
        var deleted = await _categoriasService.DeletarCategoria(id);
        return deleted ? NoContent() : NotFound();
    }
}
