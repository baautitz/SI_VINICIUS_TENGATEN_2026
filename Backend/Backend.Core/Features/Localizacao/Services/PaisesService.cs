using Backend.Core.Common;
using Backend.Core.Features.Localizacao.DTOs;
using Backend.Core.Features.Localizacao.Entities;
using Backend.Core.Features.Localizacao.Repositories;
using Backend.Core.Features.Localizacao.Validators;
using FluentValidation;

namespace Backend.Core.Features.Localizacao.Services;

public sealed class PaisesService : BaseService
{
  private readonly IPaisesRepository _paisesRepository;

  public PaisesService(IPaisesRepository paisesRepository)
  {
    _paisesRepository = paisesRepository;
  }

  public Task<ResultadoPaginado<PaisResumoDto>> ObterPaises(string? search, int pagina = 1, int tamanhoPagina = 20)
      => _paisesRepository.ObterPaises(search, pagina, tamanhoPagina);

  public Task<Paises?> ObterPaisPorId(int id)
      => _paisesRepository.ObterPaisPorId(id);

  public async Task<Resultado<Paises>> CriarPais(CreatePaisDto dto)
  {
    var validation = new CreatePaisDtoValidator().Validate(dto);
    if (!validation.IsValid)
      return Resultado<Paises>.Falha(validation.ToResultadoErros());

    var entidadeResult = Paises.Criar(dto.Ddi, dto.SiglaIso, dto.Moeda, dto.SimboloMoeda, dto.Pais);
    if (!entidadeResult.Success)
      return entidadeResult;

    if (await _paisesRepository.ExistePais(dto.SiglaIso, dto.Pais))
      return Resultado<Paises>.Falha(new ResultadoErro("DUPLICIDADE", "Já existe um país com este nome ou sigla."));

    return await ExecuteResultAsync(async () =>
    {
      var criado = await _paisesRepository.CriarPais(entidadeResult.Data!);
      return Resultado<Paises>.Sucesso(criado);
    });
  }

  public async Task<Resultado<Paises>> AtualizarPais(int id, UpdatePaisDto dto)
  {
    var validation = new UpdatePaisDtoValidator().Validate(dto);
    if (!validation.IsValid)
      return Resultado<Paises>.Falha(validation.ToResultadoErros());

    var existente = await _paisesRepository.ObterPaisPorId(id);
    if (existente is null)
      return Resultado<Paises>.Falha(new ResultadoErro("PAIS_NAO_ENCONTRADO", "País não encontrado."));

    var updateResult = existente.AtualizarResultado(dto.Ddi, dto.SiglaIso, dto.Moeda, dto.SimboloMoeda, dto.Pais);
    if (!updateResult.Success)
      return updateResult;

    if (await _paisesRepository.ExistePais(dto.SiglaIso, dto.Pais, id))
      return Resultado<Paises>.Falha(new ResultadoErro("DUPLICIDADE", "Já existe outro país com este nome ou sigla."));

    return await ExecuteResultAsync(async () =>
    {
      var atualizado = await _paisesRepository.AtualizarPais(id, existente);
      return Resultado<Paises>.Sucesso(atualizado);
    });
  }

  public Task<bool> DeletarPais(int id)
      => _paisesRepository.DeletarPais(id);
}

