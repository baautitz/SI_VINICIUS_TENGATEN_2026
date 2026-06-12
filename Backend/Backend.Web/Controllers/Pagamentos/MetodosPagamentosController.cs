using Backend.Core.Common.Results;
using Backend.Core.Features.Pagamentos.Commands;
using Backend.Core.Features.Pagamentos.Entities;
using Backend.Core.Features.Pagamentos.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace Backend.Web.Controllers.Pagamentos;

[ApiController]
[Route("api/pagamentos/metodos")]
public class MetodosPagamentosController : ControllerBase
{
    private readonly MetodosPagamentosService _metodosService;

    public MetodosPagamentosController(MetodosPagamentosService metodosService)
    {
        _metodosService = metodosService;
    }

    [HttpGet]
    public Task<ResultadoPaginado<MetodosPagamentos>> GetMetodos([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (!string.IsNullOrWhiteSpace(search))
            return _metodosService.PesquisarMetodosPagamentos(search, page, pageSize);

        return _metodosService.ObterMetodosPagamentos(page, pageSize);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MetodosPagamentos>> GetMetodo(int id)
    {
        var metodo = await _metodosService.ObterMetodoPagamentoPorId(id);
        if (metodo is null)
            return NotFound();

        return Ok(metodo);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Resultado<MetodosPagamentos>), StatusCodes.Status201Created)]
    public async Task<ActionResult<Resultado<MetodosPagamentos>>> CreateMetodo([FromBody] CriarMetodoPagamentoCommand command)
    {
        var result = await _metodosService.CriarMetodoPagamento(command);
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetMetodo), new { id = result.Data!.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Resultado<MetodosPagamentos>>> UpdateMetodo(int id, [FromBody] AtualizarMetodoPagamentoCommand command)
    {
        var result = await _metodosService.AtualizarMetodoPagamento(id, command);
        if (!result.Success)
        {
            if (result.Errors is not null && result.Errors.Any(error => error.Code == "METODO_NAO_ENCONTRADO"))
                return NotFound(result);

            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteMetodo(int id)
    {
        var deleted = await _metodosService.DeletarMetodoPagamento(id);
        return deleted ? NoContent() : NotFound();
    }
}
