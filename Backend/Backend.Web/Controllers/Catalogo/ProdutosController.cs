using Backend.Core.Common.Results;
using Backend.Core.Features.Catalogo.Commands;
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

    private static readonly System.Text.Json.JsonSerializerOptions _jsonOptions = new()
    {
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
    };

    [HttpGet]
    [ProducesResponseType(typeof(ResultadoPaginado<Produtos>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProdutos([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _produtosService.ObterProdutos(search, page, pageSize);
        return new JsonResult(result, _jsonOptions);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Produtos), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProduto(int id)
    {
        var produto = await _produtosService.ObterProdutoPorId(id);
        if (produto is null)
            return NotFound();

        return new JsonResult(produto, _jsonOptions);
    }


    [HttpPost]
    [ProducesResponseType(typeof(Resultado<Produtos>), StatusCodes.Status201Created)]
    public async Task<ActionResult<Resultado<Produtos>>> CreateProduto([FromBody] CriarProdutoCommand command)
    {
        var result = await _produtosService.CriarProduto(command);
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetProduto), new { id = result.Data!.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<Resultado<Produtos>>> UpdateProduto(int id, [FromBody] AtualizarProdutoCommand command)
    {
        var result = await _produtosService.AtualizarProduto(id, command);
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
