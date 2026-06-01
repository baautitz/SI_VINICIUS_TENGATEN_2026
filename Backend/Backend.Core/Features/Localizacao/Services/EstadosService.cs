using Backend.Core.Common;
using Backend.Core.Features.Localizacao.DTOs;
using Backend.Core.Features.Localizacao.Entities;
using Backend.Core.Features.Localizacao.Repositories;
using Backend.Core.Features.Localizacao.Validators;
using FluentValidation;

namespace Backend.Core.Features.Localizacao.Services;

public sealed class EstadosService : BaseService
{
  private readonly IEstadosRepository _estadosRepository;
  private readonly IPaisesRepository _paisesRepository;

  public EstadosService(IEstadosRepository estadosRepository, IPaisesRepository paisesRepository)
  {
    _estadosRepository = estadosRepository;
    _paisesRepository = paisesRepository;
  }

  public Task<ResultadoPaginado<EstadoResumoDto>> ObterEstados(string? search, int pagina = 1, int tamanhoDaPagina = 20)
      => _estadosRepository.ObterEstados(search, pagina, tamanhoDaPagina);

  public Task<Estados?> ObterEstadoPorId(int id)
      => _estadosRepository.ObterEstadoPorId(id);

  public async Task<Resultado<Estados>> CriarEstado(CreateEstadoDto dto)
  {
    var validation = new CreateEstadoDtoValidator().Validate(dto);
    if (!validation.IsValid)
      return Resultado<Estados>.Falha(validation.ToResultadoErros());

    var pais = await _paisesRepository.ObterPaisPorId(dto.PaisId);
    if (pais is null)
      return Resultado<Estados>.Falha(new ResultadoErro("PAIS_NAO_ENCONTRADO", "País não encontrado."));

    var entidadeResult = Estados.Criar(dto.Estado, dto.Uf, pais);
    if (!entidadeResult.Success)
      return entidadeResult;

    if (await _estadosRepository.ExisteEstado(pais.Id, dto.Uf))
      return Resultado<Estados>.Falha(new ResultadoErro("DUPLICIDADE", "Já existe um estado com esta UF neste país."));

    return await ExecuteResultAsync(async () =>
    {
      var criado = await _estadosRepository.CriarEstado(entidadeResult.Data!);
      return Resultado<Estados>.Sucesso(criado);
    });
  }

  public async Task<Resultado<Estados>> AtualizarEstado(int id, UpdateEstadoDto dto)
  {
    var validation = new UpdateEstadoDtoValidator().Validate(dto);
    if (!validation.IsValid)
      return Resultado<Estados>.Falha(validation.ToResultadoErros());

    var existente = await _estadosRepository.ObterEstadoPorId(id);
    if (existente is null)
      return Resultado<Estados>.Falha(new ResultadoErro("ESTADO_NAO_ENCONTRADO", "Estado não encontrado."));

    var pais = await _paisesRepository.ObterPaisPorId(dto.PaisId);
    if (pais is null)
      return Resultado<Estados>.Falha(new ResultadoErro("PAIS_NAO_ENCONTRADO", "País não encontrado."));

    var updateResult = existente.AtualizarResultado(dto.Estado, dto.Uf, pais);
    if (!updateResult.Success)
      return updateResult;

    if (await _estadosRepository.ExisteEstado(pais.Id, dto.Uf, id))
      return Resultado<Estados>.Falha(new ResultadoErro("DUPLICIDADE", "Já existe outro estado com esta UF neste país."));

    return await ExecuteResultAsync(async () =>
    {
      var atualizado = await _estadosRepository.AtualizarEstado(id, existente);
      return Resultado<Estados>.Sucesso(atualizado);
    });
  }

  public Task<bool> DeletarEstado(int id)
      => _estadosRepository.DeletarEstado(id);
}

