using Backend.Core.Common.Results;
using Backend.Core.Features.Parceiros.DTOs;
using Backend.Core.Features.Parceiros.Entities;
using Backend.Core.Features.Parceiros.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Web.Controllers.Parceiros;

[ApiController]
[Route("api/parceiros/emitentes")]
public class EmitentesController : ControllerBase
{
    private readonly EmitentesService _emitentesService;

    public EmitentesController(EmitentesService emitentesService)
    {
        _emitentesService = emitentesService;
    }

    [HttpGet]
    public Task<ResultadoPaginado<EmitentesResumo>> GetEmitentes([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (!string.IsNullOrWhiteSpace(search))
            return _emitentesService.PesquisarEmitentes(search, page, pageSize);

        return _emitentesService.ObterEmitentesResumo(page, pageSize);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Emitentes>> GetEmitente(int id)
    {
        var emitente = await _emitentesService.ObterEmitentePorId(id);
        if (emitente is null)
            return NotFound();

        return Ok(emitente);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Resultado<Emitentes>), StatusCodes.Status201Created)]
    public async Task<ActionResult<Resultado<Emitentes>>> CreateEmitente([FromBody] CreateEmitenteDto dto)
    {
        var result = await _emitentesService.CriarEmitente(dto);
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetEmitente), new { id = result.Data!.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Resultado<Emitentes>>> UpdateEmitente(int id, [FromBody] UpdateEmitenteDto dto)
    {
        var result = await _emitentesService.AtualizarEmitente(id, dto);
        if (!result.Success)
        {
            if (result.Errors is not null && result.Errors.Any(error => error.Code == "EMITENTE_NAO_ENCONTRADO"))
                return NotFound(result);

            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteEmitente(int id)
    {
        var deleted = await _emitentesService.DeletarEmitente(id);
        return deleted ? NoContent() : NotFound();
    }
}
