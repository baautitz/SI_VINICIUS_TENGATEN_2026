using Backend.Core.Common.Results;
using Backend.Core.Features.Financeiro.Commands;
using Backend.Core.Features.Financeiro.DTOs;
using Backend.Core.Features.Financeiro.Entities;
using Backend.Core.Features.Financeiro.Entities.Enums;
using Backend.Core.Features.Financeiro.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
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
    public Task<ResultadoPaginado<ContasPagar>> GetContas([FromQuery] string? search, [FromQuery] FiltroContasPagar filtro, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => _service.PesquisarContasPagar(search ?? "", page, pageSize, filtro);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ContasPagar>> GetConta(int id)
    {
        var conta = await _service.ObterContaPagarPorId(id);
        if (conta is null)
            return NotFound(Resultado<ContasPagar>.Falha(new ResultadoErro("CONTA_PAGAR_NAO_ENCONTRADA", "Conta a pagar não encontrada.")));

        return Ok(conta);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Resultado<IReadOnlyList<ContasPagar>>), StatusCodes.Status201Created)]
    public async Task<ActionResult<Resultado<IReadOnlyList<ContasPagar>>>> CreateConta([FromBody] CriarContaPagarCommand command)
    {
        var result = await _service.CriarContaPagar(command);
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetConta), new { id = result.Data![0].Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Resultado<ContasPagar>>> UpdateConta(int id, [FromBody] AtualizarContaPagarCommand command)
        => Responder(await _service.AtualizarContaPagar(id, command));

    [HttpPost("{id:int}/pagar")]
    public async Task<ActionResult<Resultado<ContasPagar>>> RegistrarPagamento(int id, [FromBody] PagarContaPagarCommand command)
        => Responder(await _service.RegistrarPagamento(id, command));

    [HttpPost("{id:int}/estornar")]
    public async Task<ActionResult<Resultado<ContasPagar>>> EstornarPagamento(int id, [FromBody] EstornarContaPagarCommand command)
        => Responder(await _service.EstornarPagamento(id, command));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteConta(int id)
    {
        var deleted = await _service.DeletarContaPagar(id);
        return deleted ? NoContent() : NotFound(Resultado<ContasPagar>.Falha(new ResultadoErro("CONTA_PAGAR_NAO_ENCONTRADA", "Conta a pagar não encontrada.")));
    }

    private ActionResult<Resultado<ContasPagar>> Responder(Resultado<ContasPagar> result)
    {
        if (result.Success)
            return Ok(result);

        if (result.Errors is not null && result.Errors.Any(error => error.Code == "CONTA_PAGAR_NAO_ENCONTRADA"))
            return NotFound(result);

        return BadRequest(result);
    }
}
