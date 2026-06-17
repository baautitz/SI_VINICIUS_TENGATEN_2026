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
[Route("api/financeiro/contas-receber")]
public class ContasReceberController : ControllerBase
{
    private readonly ContasReceberService _service;

    public ContasReceberController(ContasReceberService service)
    {
        _service = service;
    }

    [HttpGet]
    public Task<ResultadoPaginado<ContasReceber>> GetContas([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (!string.IsNullOrWhiteSpace(search))
            return _service.PesquisarContasReceber(search, page, pageSize);

        return _service.ObterContasReceber(page, pageSize);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ContasReceber>> GetConta(int id)
    {
        var conta = await _service.ObterContaReceberPorId(id);
        if (conta is null)
            return NotFound();

        return Ok(conta);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Resultado<ContasReceber>), StatusCodes.Status201Created)]
    public async Task<ActionResult<Resultado<ContasReceber>>> CreateConta([FromBody] CriarContaReceberCommand command)
    {
        var result = await _service.CriarContaReceber(command);
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetConta), new { id = result.Data!.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Resultado<ContasReceber>>> UpdateConta(int id, [FromBody] AtualizarContaReceberCommand command)
    {
        var result = await _service.AtualizarContaReceber(id, command);
        if (!result.Success)
        {
            if (result.Errors is not null && result.Errors.Any(error => error.Code == "CONTA_RECEBER_NAO_ENCONTRADA"))
                return NotFound(result);

            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("{id:int}/parcelas/{numeroParcela:int}/receber")]
    public async Task<ActionResult<Resultado<ContasReceber>>> RegistrarRecebimento(int id, int numeroParcela, [FromBody] RegistrarRecebimentoParcelaCommand command)
    {
        if (command.NumeroParcela != numeroParcela)
            return BadRequest("O número da parcela no path não corresponde ao número do corpo da requisição.");

        var result = await _service.RegistrarRecebimento(id, command);
        if (!result.Success)
        {
            if (result.Errors is not null && result.Errors.Any(error => error.Code == "CONTA_RECEBER_NAO_ENCONTRADA"))
                return NotFound(result);

            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteConta(int id)
    {
        var deleted = await _service.DeletarContaReceber(id);
        return deleted ? NoContent() : NotFound();
    }
}
