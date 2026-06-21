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
[Route("api/financeiro/contas-pagar")]
public class ContasPagarController : ControllerBase
{
    private readonly ContasPagarService _service;

    public ContasPagarController(ContasPagarService service)
    {
        _service = service;
    }

    [HttpGet]
    public Task<ResultadoPaginado<ContasPagar>> GetContas([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (!string.IsNullOrWhiteSpace(search))
            return _service.PesquisarContasPagar(search, page, pageSize);

        return _service.ObterContasPagar(page, pageSize);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ContasPagar>> GetConta(int id)
    {
        var conta = await _service.ObterContaPagarPorId(id);
        if (conta is null)
            return NotFound();

        return Ok(conta);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Resultado<ContasPagar>), StatusCodes.Status201Created)]
    public async Task<ActionResult<Resultado<ContasPagar>>> CreateConta([FromBody] CriarContaPagarCommand command)
    {
        var result = await _service.CriarContaPagar(command);
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetConta), new { id = result.Data!.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Resultado<ContasPagar>>> UpdateConta(int id, [FromBody] AtualizarContaPagarCommand command)
    {
        var result = await _service.AtualizarContaPagar(id, command);
        if (!result.Success)
        {
            if (result.Errors is not null && result.Errors.Any(error => error.Code == "CONTA_PAGAR_NAO_ENCONTRADA"))
                return NotFound(result);

            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("{id:int}/parcelas/{numeroParcela:int}/pagar")]
    public async Task<ActionResult<Resultado<ContasPagar>>> RegistrarPagamento(int id, int numeroParcela, [FromBody] RegistrarPagamentoParcelaCommand command)
    {
        if (command.NumeroParcela != numeroParcela)
            return BadRequest("O número da parcela no path não corresponde ao número do corpo da requisição.");

        var result = await _service.RegistrarPagamento(id, command);
        if (!result.Success)
        {
            if (result.Errors is not null && result.Errors.Any(error => error.Code == "CONTA_PAGAR_NAO_ENCONTRADA"))
                return NotFound(result);

            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("{id:int}/parcelas/{numeroParcela:int}/estornar")]
    public async Task<ActionResult<Resultado<ContasPagar>>> EstornarPagamento(int id, int numeroParcela, [FromBody] EstornarPagamentoParcelaCommand command)
    {
        if (command.NumeroParcela != numeroParcela)
            return BadRequest("O número da parcela no path não corresponde ao número do corpo da requisição.");

        var result = await _service.EstornarPagamento(id, command);
        if (!result.Success)
        {
            if (result.Errors is not null && result.Errors.Any(error => error.Code == "CONTA_PAGAR_NAO_ENCONTRADA"))
                return NotFound(result);

            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteConta(int id)
    {
        var deleted = await _service.DeletarContaPagar(id);
        return deleted ? NoContent() : NotFound();
    }
}
