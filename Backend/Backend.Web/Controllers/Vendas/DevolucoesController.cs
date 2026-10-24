using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend.Core.Common.Results;
using Backend.Core.Features.Vendas.Commands;
using Backend.Core.Features.Vendas.DTOs;
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

    [HttpGet("api/devolucoes")]
    public Task<ResultadoPaginado<DevolucaoResumo>> GetDevolucoes([FromQuery] string? search, [FromQuery] int? vendaId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => _vendasService.PesquisarDevolucoes(search ?? "", page, pageSize, vendaId);

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

    [HttpPost("api/devolucoes/{id:int}/estornar")]
    public async Task<ActionResult<Resultado<Devolucao>>> Estornar(int id, [FromBody] EstornarDevolucaoCommand command)
    {
        var result = await _vendasService.EstornarDevolucao(id, command);
        if (result.Success)
            return Ok(result);
        if (result.Errors!.Any(e => e.Code == VendasService.DevolucaoInexistente))
            return NotFound(result);
        return BadRequest(result);
    }
}
