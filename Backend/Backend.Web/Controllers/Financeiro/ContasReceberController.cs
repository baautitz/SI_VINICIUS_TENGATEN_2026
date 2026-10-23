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
[Route("api/financeiro/contas-receber")]
public class ContasReceberController : ControllerBase
{
    private readonly ContasReceberService _service;

    public ContasReceberController(ContasReceberService service)
    {
        _service = service;
    }

    [HttpGet]
    public Task<ResultadoPaginado<ContasReceber>> GetContas([FromQuery] string? search, [FromQuery] int? clienteId, [FromQuery] OrigemTituloFinanceiro? origemTipo, [FromQuery] int? origemId, [FromQuery] string? numeroDocumento, [FromQuery] StatusTituloFinanceiro? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => _service.PesquisarContasReceber(search ?? "", page, pageSize,
            new FiltroContasReceber(clienteId, origemTipo, origemId, numeroDocumento, status));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ContasReceber>> GetConta(int id)
    {
        var conta = await _service.ObterContaReceberPorId(id);
        if (conta is null)
            return NotFound(Resultado<ContasReceber>.Falha(new ResultadoErro("CONTA_RECEBER_NAO_ENCONTRADA", "Conta a receber não encontrada.")));

        return Ok(conta);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Resultado<IReadOnlyList<ContasReceber>>), StatusCodes.Status201Created)]
    public async Task<ActionResult<Resultado<IReadOnlyList<ContasReceber>>>> CreateConta([FromBody] CriarContaReceberCommand command)
    {
        var result = await _service.CriarContaReceber(command);
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetConta), new { id = result.Data![0].Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Resultado<ContasReceber>>> UpdateConta(int id, [FromBody] AtualizarContaReceberCommand command)
        => Responder(await _service.AtualizarContaReceber(id, command));

    [HttpPost("{id:int}/receber")]
    public async Task<ActionResult<Resultado<ContasReceber>>> RegistrarRecebimento(int id, [FromBody] ReceberContaReceberCommand command)
        => Responder(await _service.RegistrarRecebimento(id, command));

    [HttpPost("{id:int}/estornar")]
    public async Task<ActionResult<Resultado<ContasReceber>>> EstornarRecebimento(int id, [FromBody] EstornarContaReceberCommand command)
        => Responder(await _service.EstornarRecebimento(id, command));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteConta(int id)
    {
        var deleted = await _service.DeletarContaReceber(id);
        return deleted ? NoContent() : NotFound(Resultado<ContasReceber>.Falha(new ResultadoErro("CONTA_RECEBER_NAO_ENCONTRADA", "Conta a receber não encontrada.")));
    }

    private ActionResult<Resultado<ContasReceber>> Responder(Resultado<ContasReceber> result)
    {
        if (result.Success)
            return Ok(result);

        if (result.Errors is not null && result.Errors.Any(error => error.Code == "CONTA_RECEBER_NAO_ENCONTRADA"))
            return NotFound(result);

        return BadRequest(result);
    }
}
