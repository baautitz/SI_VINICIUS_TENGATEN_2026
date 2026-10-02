using Backend.Core.Features.Relacionados;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Web.Controllers;

[ApiController]
[Route("api/relacionados")]
public class RelacionadosController : ControllerBase
{
    private readonly IRelacionadosRepository _repo;

    public RelacionadosController(IRelacionadosRepository repo) => _repo = repo;

    [HttpGet("contas-receber/venda/{id:int}")]
    public async Task<IReadOnlyList<RelacionadoItem>> ContasReceberPorVenda(int id) => await _repo.ContasReceberPorVenda(id);

    [HttpGet("movimentacoes/venda/{id:int}")]
    public async Task<IReadOnlyList<RelacionadoItem>> MovimentacoesPorVenda(int id) => await _repo.MovimentacoesPorVenda(id);

    [HttpGet("contas-receber/cliente/{id:int}")]
    public async Task<IReadOnlyList<RelacionadoItem>> ContasReceberPorCliente(int id) => await _repo.ContasReceberPorCliente(id);

    [HttpGet("contas-pagar/fornecedor/{id:int}")]
    public async Task<IReadOnlyList<RelacionadoItem>> ContasPagarPorFornecedor(int id) => await _repo.ContasPagarPorFornecedor(id);

    [HttpGet("vendas/cliente/{id:int}")]
    public async Task<IReadOnlyList<RelacionadoItem>> VendasPorCliente(int id) => await _repo.VendasPorCliente(id);

    [HttpGet("produtos/{campo}/{id:int}")]
    public async Task<ActionResult<IReadOnlyList<RelacionadoItem>>> Produtos(string campo, int id)
    {
        try { return Ok(await _repo.ProdutosPor(campo, id)); }
        catch (ArgumentException e) { return BadRequest(e.Message); }
    }

    [HttpGet("localizacao/{pai}/{id:int}")]
    public async Task<ActionResult<IReadOnlyList<RelacionadoItem>>> Localizacao(string pai, int id)
    {
        try { return Ok(await _repo.LocalizacaoFilhos(pai, id)); }
        catch (ArgumentException e) { return BadRequest(e.Message); }
    }
}
