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
[Route("api/pagamentos/condicoes")]
public class CondicoesPagamentosController : ControllerBase
{
    private readonly CondicoesPagamentosService _condicoesService;

    public CondicoesPagamentosController(CondicoesPagamentosService condicoesService)
    {
        _condicoesService = condicoesService;
    }

    [HttpGet]
    public Task<ResultadoPaginado<CondicoesPagamentos>> GetCondicoes([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (!string.IsNullOrWhiteSpace(search))
            return _condicoesService.PesquisarCondicoesPagamentos(search, page, pageSize);

        return _condicoesService.ObterCondicoesPagamentos(page, pageSize);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CondicoesPagamentos>> GetCondicao(int id)
    {
        var condicao = await _condicoesService.ObterCondicaoPagamentoPorId(id);
        if (condicao is null)
            return NotFound();

        return Ok(condicao);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Resultado<CondicoesPagamentos>), StatusCodes.Status201Created)]
    public async Task<ActionResult<Resultado<CondicoesPagamentos>>> CreateCondicao([FromBody] CriarCondicaoPagamentoCommand command)
    {
        var result = await _condicoesService.CriarCondicaoPagamento(command);
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetCondicao), new { id = result.Data!.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Resultado<CondicoesPagamentos>>> UpdateCondicao(int id, [FromBody] AtualizarCondicaoPagamentoCommand command)
    {
        var result = await _condicoesService.AtualizarCondicaoPagamento(id, command);
        if (!result.Success)
        {
            if (result.Errors is not null && result.Errors.Any(error => error.Code == "CONDICAO_NAO_ENCONTRADA"))
                return NotFound(result);

            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCondicao(int id)
    {
        var deleted = await _condicoesService.DeletarCondicaoPagamento(id);
        return deleted ? NoContent() : NotFound();
    }
}
