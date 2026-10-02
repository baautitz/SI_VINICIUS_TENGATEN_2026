using Backend.Core.Common.Results;
using Backend.Core.Features.Estoque.Commands;
using Backend.Core.Features.Estoque.Entities;
using Backend.Core.Features.Estoque.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Threading.Tasks;

namespace Backend.Web.Controllers.Estoque;

[ApiController]
[Route("api/estoque/movimentacoes")]
public class MovimentacoesEstoquesController : ControllerBase
{
    private readonly MovimentacoesEstoquesService _movimentacoesService;

    public MovimentacoesEstoquesController(MovimentacoesEstoquesService movimentacoesService)
    {
        _movimentacoesService = movimentacoesService;
    }

    [HttpGet]
    public Task<ResultadoPaginado<MovimentacoesEstoques>> GetMovimentacoes([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => _movimentacoesService.ObterMovimentacoes(search, page, pageSize);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<MovimentacoesEstoques>> GetMovimentacao(int id)
    {
        var movimentacao = await _movimentacoesService.ObterMovimentacaoPorId(id);
        return movimentacao is null ? NotFound() : Ok(movimentacao);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Resultado<MovimentacoesEstoques>), StatusCodes.Status201Created)]
    public async Task<ActionResult<Resultado<MovimentacoesEstoques>>> CreateMovimentacao([FromBody] CriarMovimentacaoCommand command)
    {
        var result = await _movimentacoesService.CriarMovimentacao(command);
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetMovimentacao), new { id = result.Data!.Id }, result);
    }

    [HttpPost("{id:int}/estornar")]
    public async Task<ActionResult<Resultado<MovimentacoesEstoques>>> EstornarMovimentacao(int id, [FromBody] EstornarMovimentacaoCommand command)
    {
        var result = await _movimentacoesService.Estornar(id, command);
        if (!result.Success)
        {
            if (result.Errors is not null && result.Errors.Any(error => error.Code == "MOVIMENTACAO_INEXISTENTE"))
                return NotFound(result);

            return BadRequest(result);
        }

        return Ok(result);
    }
}
