using Backend.Core.Common.Results;
using Backend.Core.Features.Catalogo.DTOs;
using Backend.Core.Features.Catalogo.Entities;
using Backend.Core.Features.Catalogo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Threading.Tasks;

namespace Backend.Web.Controllers.Catalogo;

[ApiController]
[Route("api/catalogo/produtos")]
public class ProdutosController : ControllerBase
{
    private readonly ProdutosService _produtosService;

    public ProdutosController(ProdutosService produtosService)
    {
        _produtosService = produtosService;
    }

    [HttpGet]
    public Task<ResultadoPaginado<ProdutosResumo>> GetProdutos([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => _produtosService.ObterProdutos(search, page, pageSize);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Produtos>> GetProduto(int id)
    {
        var produto = await _produtosService.ObterProdutoPorId(id);
        if (produto is null)
            return NotFound();

        return Ok(produto);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Resultado<Produtos>), StatusCodes.Status201Created)]
    public async Task<ActionResult<Resultado<Produtos>>> CreateProduto([FromBody] CreateProdutoDto dto)
    {
        var result = await _produtosService.CriarProduto(dto);
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetProduto), new { id = result.Data!.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Resultado<Produtos>>> UpdateProduto(int id, [FromBody] UpdateProdutoDto dto)
    {
        var result = await _produtosService.AtualizarProduto(id, dto);
        if (!result.Success)
        {
            if (result.Errors is not null && result.Errors.Any(error => error.Code == "PRODUTO_NAO_ENCONTRADO"))
                return NotFound(result);

            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteProduto(int id)
    {
        var deleted = await _produtosService.DeletarProduto(id);
        return deleted ? NoContent() : NotFound();
    }
}
