using Backend.Core.Common.Results;
using Backend.Core.Features.NFe.Commands;
using Backend.Core.Features.NFe.DTOs;
using Backend.Core.Features.NFe.Entities;
using Backend.Core.Features.NFe.Entities.Enums;
using Backend.Core.Features.NFe.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Web.Controllers.NFe;

[ApiController]
public class NfesController : ControllerBase
{
    private readonly NfesImportacaoService _importacaoService;

    private readonly NfesSaidaService _saidaService;

    public NfesController(NfesImportacaoService importacaoService, NfesSaidaService saidaService)
    {
        _importacaoService = importacaoService;
        _saidaService = saidaService;
    }

    // Listagem paginada de resumos (sem xml_documento). Filtros: tipoOperacao (ENTRADA/SAIDA), statusNfe, search.
    [HttpGet("api/nfes")]
    public Task<ResultadoPaginado<NfesResumo>> GetNfes(
        [FromQuery] string? search, [FromQuery] TipoOperacao? tipoOperacao, [FromQuery] StatusNfe? statusNfe,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        => _importacaoService.PesquisarNfes(search, Math.Max(page, 1), Math.Clamp(pageSize, 1, 100), tipoOperacao, statusNfe);

    [HttpGet("api/nfes/{id:int}")]
    public async Task<ActionResult<Nfes>> GetNfe(int id)
    {
        var nfe = await _importacaoService.ObterNfePorId(id);
        return nfe is null ? NotFound() : Ok(nfe);
    }

    // XML original importado (NF-e de entrada); 404 se a NFe não existe ou não tem XML guardado.
    [HttpGet("api/nfes/{id:int}/xml")]
    public async Task<IActionResult> GetXml(int id)
    {
        var xml = await _importacaoService.ObterXmlDocumento(id);
        return xml is null ? NotFound() : Content(xml, "application/xml");
    }

    // Stateless: lê o XML e devolve o que seria importado; nada é gravado.
    [HttpPost("api/nfes/importar/previa")]
    [RequestSizeLimit(3 * 1024 * 1024)]
    public async Task<ActionResult<Resultado<PreviaNfeDto>>> Previa([FromBody] PreviaNfeCommand command)
    {
        var result = await _importacaoService.Previa(command);
        return result.Success ? Ok(result) : Falha(result.Errors!, result);
    }

    [HttpPost("api/nfes/importar")]
    [RequestSizeLimit(3 * 1024 * 1024)]
    [ProducesResponseType(typeof(Resultado<ImportarNfeResultado>), StatusCodes.Status201Created)]
    public async Task<ActionResult<Resultado<ImportarNfeResultado>>> Importar([FromBody] ImportarNfeCommand command)
    {
        var result = await _importacaoService.Importar(command);
        if (!result.Success)
            return Falha(result.Errors!, result);

        return CreatedAtAction(nameof(GetNfe), new { id = result.Data!.NfeId }, result);
    }

    [HttpPost("api/vendas/{vendaId:int}/nfe")]
    [ProducesResponseType(typeof(Resultado<Nfes>), StatusCodes.Status201Created)]
    public async Task<ActionResult<Resultado<Nfes>>> EmitirDeVenda(int vendaId)
    {
        var result = await _saidaService.EmitirDeVenda(vendaId);
        if (!result.Success)
            return Falha(result.Errors!, result);

        return CreatedAtAction(nameof(GetNfe), new { id = result.Data!.Id }, result);
    }

    [HttpPost("api/nfes/{id:int}/cancelar")]
    public async Task<ActionResult<Resultado<Nfes>>> Cancelar(int id, [FromBody] CancelarNfeCommand command)
    {
        var result = await _saidaService.Cancelar(id, command);
        return result.Success ? Ok(result) : Falha(result.Errors!, result);
    }

    // Chave já importada = 409; demais falhas = 400.
    private ActionResult Falha<T>(IReadOnlyCollection<ResultadoErro> erros, Resultado<T> result)
        => erros.Any(e => e.Code == NfesImportacaoService.ChaveJaImportada) || NfesSaidaService.EhConflito(erros) ? Conflict(result) : BadRequest(result);
}
