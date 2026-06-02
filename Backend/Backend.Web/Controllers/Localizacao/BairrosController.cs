using Backend.Core.Common.Results;
using Backend.Core.Features.Localizacao.DTOs;
using Backend.Core.Features.Localizacao.Entities;
using Backend.Core.Features.Localizacao.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Web.Controllers.Localizacao;

[ApiController]
[Route("api/localizacao/bairros")]
public class BairrosController : ControllerBase
{
  private readonly BairrosService _bairrosService;

  public BairrosController(BairrosService bairrosService)
  {
    _bairrosService = bairrosService;
  }

  [HttpGet]
  public Task<ResultadoPaginado<BairroResumoDto>> GetBairros([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
      => _bairrosService.ObterBairros(search, page, pageSize);

  [HttpGet("{id:int}")]
  public async Task<ActionResult<Bairros>> GetBairro(int id)
  {
    var bairro = await _bairrosService.ObterBairroPorId(id);
    if (bairro is null)
      return NotFound();

    return Ok(bairro);
  }

  [HttpPost]
  [ProducesResponseType(typeof(Resultado<Bairros>), StatusCodes.Status201Created)]
  public async Task<ActionResult<Resultado<Bairros>>> CreateBairro([FromBody] CreateBairroDto dto)
  {
    var result = await _bairrosService.CriarBairro(dto);
    if (!result.Success)
      return BadRequest(result);

    return CreatedAtAction(nameof(GetBairro), new { id = result.Data!.Id }, result);
  }

  [HttpPut("{id:int}")]
  public async Task<ActionResult<Resultado<Bairros>>> UpdateBairro(int id, [FromBody] UpdateBairroDto dto)
  {
    var result = await _bairrosService.AtualizarBairro(id, dto);
    if (!result.Success)
    {
      if (result.Errors is not null && result.Errors.Any(error => error.Code == "BAIRRO_NAO_ENCONTRADO"))
        return NotFound(result);

      if (result.Errors is not null && result.Errors.Any(error => error.Code == "CIDADE_NAO_ENCONTRADA"))
        return BadRequest(result);

      return BadRequest(result);
    }

    return Ok(result);
  }

  [HttpDelete("{id:int}")]
  public async Task<IActionResult> DeleteBairro(int id)
  {
    var deleted = await _bairrosService.DeletarBairro(id);
    return deleted ? NoContent() : NotFound();
  }
}
