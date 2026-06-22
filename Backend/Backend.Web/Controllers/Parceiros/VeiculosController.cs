using Backend.Core.Common.Results;
using Backend.Core.Features.Parceiros.Commands;
using Backend.Core.Features.Parceiros.Entities;
using Backend.Core.Features.Parceiros.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Web.Controllers.Parceiros;

[ApiController]
[Route("api/parceiros/veiculos")]
public class VeiculosController : ControllerBase
{
    private readonly VeiculosService _veiculosService;

    public VeiculosController(VeiculosService veiculosService)
    {
        _veiculosService = veiculosService;
    }

    [HttpGet]
    public Task<ResultadoPaginado<Veiculos>> GetVeiculos([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (!string.IsNullOrWhiteSpace(search))
            return _veiculosService.PesquisarVeiculos(search, page, pageSize);

        return _veiculosService.ObterVeiculos(page, pageSize);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Veiculos>> GetVeiculo(int id)
    {
        var veiculo = await _veiculosService.ObterVeiculoPorId(id);
        if (veiculo is null)
            return NotFound();

        return Ok(veiculo);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Resultado<Veiculos>), StatusCodes.Status201Created)]
    public async Task<ActionResult<Resultado<Veiculos>>> CreateVeiculo([FromBody] CriarVeiculoCommand command)
    {
        var result = await _veiculosService.CriarVeiculo(command);
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetVeiculo), new { id = result.Data!.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Resultado<Veiculos>>> UpdateVeiculo(int id, [FromBody] AtualizarVeiculoCommand command)
    {
        var result = await _veiculosService.AtualizarVeiculo(id, command);
        if (!result.Success)
        {
            if (result.Errors is not null && result.Errors.Any(error => error.Code == "VEICULO_NAO_ENCONTRADO"))
                return NotFound(result);

            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteVeiculo(int id)
    {
        var deleted = await _veiculosService.DeletarVeiculo(id);
        return deleted ? NoContent() : NotFound();
    }
}

