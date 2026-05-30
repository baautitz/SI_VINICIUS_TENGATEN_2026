using Backend.Core.Common;
using Backend.Core.Features.Localizacao.DTOs;
using Backend.Core.Features.Localizacao.Entities;
using Backend.Core.Features.Localizacao.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Web.Controllers.Localizacao;

[ApiController]
[Route("api/localizacao/cidades")]
public class CidadesController : ControllerBase
{
  private readonly CidadesService _cidadesService;

  public CidadesController(CidadesService cidadesService)
  {
    _cidadesService = cidadesService;
  }

  [HttpGet]
  public Task<ResultadoPaginado<Cidades>> GetCidades([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
      => _cidadesService.ObterCidades(page, pageSize);

  [HttpGet("{id:int}")]
  public async Task<ActionResult<Cidades>> GetCidade(int id)
  {
    var cidade = await _cidadesService.ObterCidadePorId(id);
    if (cidade is null)
      return NotFound();

    return Ok(cidade);
  }

  [HttpPost]
  public async Task<ActionResult<Resultado<Cidades>>> CreateCidade([FromBody] CreateCidadeDto dto)
  {
    var result = await _cidadesService.CriarCidade(dto);
    if (!result.Success)
      return BadRequest(result);

    return CreatedAtAction(nameof(GetCidade), new { id = result.Data!.Id }, result);
  }

  [HttpPut("{id:int}")]
  public async Task<ActionResult<Resultado<Cidades>>> UpdateCidade(int id, [FromBody] UpdateCidadeDto dto)
  {
    var result = await _cidadesService.AtualizarCidade(id, dto);
    if (!result.Success)
    {
      if (result.Errors is not null && result.Errors.Any(error => error.Code == "CIDADE_NAO_ENCONTRADA"))
        return NotFound(result);

      if (result.Errors is not null && result.Errors.Any(error => error.Code == "ESTADO_NAO_ENCONTRADO"))
        return BadRequest(result);

      return BadRequest(result);
    }

    return Ok(result);
  }

  [HttpDelete("{id:int}")]
  public async Task<IActionResult> DeleteCidade(int id)
  {
    var deleted = await _cidadesService.DeletarCidade(id);
    return deleted ? NoContent() : NotFound();
  }
}
