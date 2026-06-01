using Backend.Core.Common;
using Backend.Core.Features.Parceiros.DTOs;
using Backend.Core.Features.Parceiros.Entities;
using Backend.Core.Features.Parceiros.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Web.Controllers.Parceiros;

[ApiController]
[Route("api/parceiros/clientes")]
public class ClientesController : ControllerBase
{
    private readonly ClientesService _clientesService;

    public ClientesController(ClientesService clientesService)
    {
        _clientesService = clientesService;
    }

    [HttpGet]
    public Task<ResultadoPaginado<ClientesResumo>> GetClientes([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (!string.IsNullOrWhiteSpace(search))
            return _clientesService.PesquisarClientes(search, page, pageSize);

        return _clientesService.ObterClientesResumo(page, pageSize);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Clientes>> GetCliente(int id)
    {
        var cliente = await _clientesService.ObterClientePorId(id);
        if (cliente is null)
            return NotFound();

        return Ok(cliente);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Resultado<Clientes>), StatusCodes.Status201Created)]
    public async Task<ActionResult<Resultado<Clientes>>> CreateCliente([FromBody] CreateClienteDto dto)
    {
        var result = await _clientesService.CriarCliente(dto);
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetCliente), new { id = result.Data!.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Resultado<Clientes>>> UpdateCliente(int id, [FromBody] UpdateClienteDto dto)
    {
        var result = await _clientesService.AtualizarCliente(id, dto);
        if (!result.Success)
        {
            if (result.Errors is not null && result.Errors.Any(error => error.Code == "CLIENTE_NAO_ENCONTRADO"))
                return NotFound(result);

            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCliente(int id)
    {
        var deleted = await _clientesService.DeletarCliente(id);
        return deleted ? NoContent() : NotFound();
    }
}
