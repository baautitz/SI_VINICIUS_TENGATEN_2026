using System.Collections.Generic;
using System.Threading.Tasks;
using Backend.Core.Common.Results;
using Backend.Core.Features.Vendas.Commands;
using Backend.Core.Features.Vendas.Entities;
using Backend.Core.Features.Vendas.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Web.Controllers.Vendas;

[ApiController]
public class DevolucoesController : ControllerBase
{
    private readonly VendasService _vendasService;

    public DevolucoesController(VendasService vendasService)
    {
        _vendasService = vendasService;
    }

    [HttpGet("api/vendas/{vendaId:int}/devolucoes")]
    public Task<IReadOnlyList<Devolucao>> GetDevolucoesDaVenda(int vendaId)
        => _vendasService.ObterDevolucoesPorVenda(vendaId);

    [HttpGet("api/devolucoes/{id:int}")]
    public async Task<ActionResult<Devolucao>> GetDevolucao(int id)
    {
        var devolucao = await _vendasService.ObterDevolucaoPorId(id);
        return devolucao is null ? NotFound() : Ok(devolucao);
    }

    [HttpPost("api/vendas/{vendaId:int}/devolucoes")]
    [ProducesResponseType(typeof(Resultado<Devolucao>), StatusCodes.Status201Created)]
    public async Task<ActionResult<Resultado<Devolucao>>> CreateDevolucao(int vendaId, [FromBody] CriarDevolucaoCommand command)
    {
        var result = await _vendasService.CriarDevolucao(vendaId, command);
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetDevolucao), new { id = result.Data!.Id }, result);
    }
}
