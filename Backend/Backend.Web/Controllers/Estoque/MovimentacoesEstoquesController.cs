using Backend.Core.Common.Results;
using Backend.Core.Features.Estoque.Commands;
using Backend.Core.Features.Estoque.Entities;
using Backend.Core.Features.Estoque.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Threading.Tasks;
using System;

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
        if (movimentacao is null)
            return NotFound();

        return Ok(movimentacao);
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

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Resultado<MovimentacoesEstoques>>> UpdateMovimentacao(int id, [FromBody] AtualizarMovimentacaoCommand command)
    {
        var result = await _movimentacoesService.AtualizarMovimentacao(id, command);
        if (!result.Success)
        {
            if (result.Errors is not null && result.Errors.Any(error => error.Code == "MOVIMENTACAO_INEXISTENTE"))
                return NotFound(result);

            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteMovimentacao(int id)
    {
        var movimentacao = await _movimentacoesService.ObterMovimentacaoPorId(id);
        if (movimentacao is null)
            return NotFound();

        if (movimentacao.Status != Backend.Core.Features.Estoque.Entities.Enums.StatusMovimentacaoEstoque.RASCUNHO)
        {
            return BadRequest(Resultado<bool>.Falha(new ResultadoErro("MOVIMENTACAO_BLOQUEADA", "Apenas movimentações em rascunho podem ser excluídas.")));
        }

        var deleted = await _movimentacoesService.DeletarMovimentacao(id);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("{id:int}/confirmar")]
    public async Task<ActionResult<Resultado<MovimentacoesEstoques>>> ConfirmarMovimentacao(int id)
    {
        var result = await _movimentacoesService.ConfirmarMovimentacao(id);
        if (!result.Success)
        {
            if (result.Errors is not null && result.Errors.Any(error => error.Code == "MOVIMENTACAO_INEXISTENTE"))
                return NotFound(result);

            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("{id:int}/cancelar")]
    public async Task<ActionResult<Resultado<MovimentacoesEstoques>>> CancelarMovimentacao(int id)
    {
        var result = await _movimentacoesService.CancelarMovimentacao(id);
        if (!result.Success)
        {
            if (result.Errors is not null && result.Errors.Any(error => error.Code == "MOVIMENTACAO_INEXISTENTE"))
                return NotFound(result);

            return BadRequest(result);
        }

        return Ok(result);
    }
}
