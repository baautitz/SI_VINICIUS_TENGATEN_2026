using Backend.Core.Common.Results;
using Backend.Core.Features.Logistica.Commands;
using Backend.Core.Features.Logistica.Entities;
using Backend.Core.Features.Logistica.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Web.Controllers.Logistica;

[ApiController]
[Route("api/logistica/transportadoras")]
public class TransportadorasController : ControllerBase
{
    private readonly TransportadorasService _transportadorasService;

    public TransportadorasController(TransportadorasService transportadorasService)
    {
        _transportadorasService = transportadorasService;
    }

    [HttpGet]
    public Task<ResultadoPaginado<Transportadoras>> GetTransportadoras([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (!string.IsNullOrWhiteSpace(search))
            return _transportadorasService.PesquisarTransportadoras(search, page, pageSize);

        return _transportadorasService.ObterTransportadoras(page, pageSize);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Transportadoras>> GetTransportadora(int id)
    {
        var transportadora = await _transportadorasService.ObterTransportadoraPorId(id);
        if (transportadora is null)
            return NotFound();

        return Ok(transportadora);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Resultado<Transportadoras>), StatusCodes.Status201Created)]
    public async Task<ActionResult<Resultado<Transportadoras>>> CreateTransportadora([FromBody] CriarTransportadoraCommand command)
    {
        var result = await _transportadorasService.CriarTransportadora(command);
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetTransportadora), new { id = result.Data!.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Resultado<Transportadoras>>> UpdateTransportadora(int id, [FromBody] AtualizarTransportadoraCommand command)
    {
        var result = await _transportadorasService.AtualizarTransportadora(id, command);
        if (!result.Success)
        {
            if (result.Errors is not null && result.Errors.Any(error => error.Code == "TRANSPORTADORA_NAO_ENCONTRADO"))
                return NotFound(result);

            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteTransportadora(int id)
    {
        var deleted = await _transportadorasService.DeletarTransportadora(id);
        return deleted ? NoContent() : NotFound();
    }
}
