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
[Route("api/localizacao/cidades")]
public class CidadesController : ControllerBase
{
    private readonly CidadesService _cidadesService;

    public CidadesController(CidadesService cidadesService)
    {
        _cidadesService = cidadesService;
    }

    [HttpGet]
    public Task<ResultadoPaginado<Cidades>> GetCidades([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => _cidadesService.ObterCidades(search, page, pageSize);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Cidades>> GetCidade(int id)
    {
        var cidade = await _cidadesService.ObterCidadePorId(id);
        if (cidade is null)
            return NotFound();

        return Ok(cidade);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Resultado<Cidades>), StatusCodes.Status201Created)]
    public async Task<ActionResult<Resultado<Cidades>>> CreateCidade([FromBody] CriarCidadeCommand command)
    {
        var result = await _cidadesService.CriarCidade(command);
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetCidade), new { id = result.Data!.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Resultado<Cidades>>> UpdateCidade(int id, [FromBody] AtualizarCidadeCommand command)
    {
        var result = await _cidadesService.AtualizarCidade(id, command);
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
