using Backend.Core.Common.Results;
using Backend.Core.Features.Financeiro.Commands;
using Backend.Core.Features.Financeiro.Entities;
using Backend.Core.Features.Financeiro.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace Backend.Web.Controllers.Financeiro;

[ApiController]
[Route("api/financeiro/metodos")]
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

    [HttpGet("{codigo}")]
    public async Task<ActionResult<MetodosPagamentos>> GetMetodo(string codigo)
    {
        var metodo = await _metodosService.ObterMetodoPagamentoPorCodigo(codigo);
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

        return CreatedAtAction(nameof(GetMetodo), new { codigo = result.Data!.Codigo }, result);
    }

    [HttpPut("{codigo}")]
    public async Task<ActionResult<Resultado<MetodosPagamentos>>> UpdateMetodo(string codigo, [FromBody] AtualizarMetodoPagamentoCommand command)
    {
        var result = await _metodosService.AtualizarMetodoPagamento(codigo, command);
        if (!result.Success)
        {
            if (result.Errors is not null && result.Errors.Any(error => error.Code == "METODO_NAO_ENCONTRADO"))
                return NotFound(result);

            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpDelete("{codigo}")]
    public async Task<IActionResult> DeleteMetodo(string codigo)
    {
        var deleted = await _metodosService.DeletarMetodoPagamento(codigo);
        return deleted ? NoContent() : NotFound();
    }
}

