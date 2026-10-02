using Backend.Core.Common.Results;
using Backend.Core.Features.NFe.Commands;
using Backend.Core.Features.NFe.DTOs;
using Backend.Core.Features.NFe.Entities;
using Backend.Core.Features.NFe.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Web.Controllers.NFe;

[ApiController]
public class NfesController : ControllerBase
{
    private readonly NfesImportacaoService _importacaoService;

    public NfesController(NfesImportacaoService importacaoService)
    {
        _importacaoService = importacaoService;
    }

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

    // Chave já importada = 409; demais falhas = 400.
    private ActionResult Falha<T>(IReadOnlyCollection<ResultadoErro> erros, Resultado<T> result)
        => erros.Any(e => e.Code == NfesImportacaoService.ChaveJaImportada) ? Conflict(result) : BadRequest(result);
}
