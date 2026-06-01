using Backend.Core.Common;
using Backend.Core.Features.Parceiros.DTOs;
using Backend.Core.Features.Parceiros.Entities;
using Backend.Core.Features.Parceiros.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Web.Controllers.Parceiros;

[ApiController]
[Route("api/parceiros/fornecedores")]
public class FornecedoresController : ControllerBase
{
    private readonly FornecedoresService _fornecedoresService;

    public FornecedoresController(FornecedoresService fornecedoresService)
    {
        _fornecedoresService = fornecedoresService;
    }

    [HttpGet]
    public Task<ResultadoPaginado<FornecedoresResumo>> GetFornecedores([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (!string.IsNullOrWhiteSpace(search))
            return _fornecedoresService.PesquisarFornecedores(search, page, pageSize);

        return _fornecedoresService.ObterFornecedoresResumo(page, pageSize);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Fornecedores>> GetFornecedor(int id)
    {
        var fornecedor = await _fornecedoresService.ObterFornecedorPorId(id);
        if (fornecedor is null)
            return NotFound();

        return Ok(fornecedor);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Resultado<Fornecedores>), StatusCodes.Status201Created)]
    public async Task<ActionResult<Resultado<Fornecedores>>> CreateFornecedor([FromBody] CreateFornecedorDto dto)
    {
        var result = await _fornecedoresService.CriarFornecedor(dto);
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetFornecedor), new { id = result.Data!.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Resultado<Fornecedores>>> UpdateFornecedor(int id, [FromBody] UpdateFornecedorDto dto)
    {
        var result = await _fornecedoresService.AtualizarFornecedor(id, dto);
        if (!result.Success)
        {
            if (result.Errors is not null && result.Errors.Any(error => error.Code == "FORNECEDOR_NAO_ENCONTRADO"))
                return NotFound(result);

            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteFornecedor(int id)
    {
        var deleted = await _fornecedoresService.DeletarFornecedor(id);
        return deleted ? NoContent() : NotFound();
    }
}
