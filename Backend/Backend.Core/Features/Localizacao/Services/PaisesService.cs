using Backend.Core.Common;
using Backend.Core.Features.Localizacao.DTOs;
using Backend.Core.Features.Localizacao.Entities;
using Backend.Core.Features.Localizacao.Repositories;
using Backend.Core.Features.Localizacao.Validators;
using FluentValidation;

namespace Backend.Core.Features.Localizacao.Services;

public sealed class PaisesService
{
  private readonly IPaisesRepository _paisesRepository;

  public PaisesService(IPaisesRepository paisesRepository)
  {
    _paisesRepository = paisesRepository;
  }

  public Task<ResultadoPaginado<Paises>> ObterPaises(int pagina = 1, int tamanhoPagina = 20)
      => _paisesRepository.ObterPaises(pagina, tamanhoPagina);

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

    var criado = await _paisesRepository.CriarPais(entidadeResult.Data!);
    return Resultado<Paises>.Sucesso(criado);
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

    var atualizado = await _paisesRepository.AtualizarPais(id, existente);
    return Resultado<Paises>.Sucesso(atualizado);
  }

  public Task<bool> DeletarPais(int id)
      => _paisesRepository.DeletarPais(id);

  public Task<ResultadoPaginado<PaisResumoDto>> ObterPaisesResumo(int pagina = 1, int tamanhoPagina = 20)
      => _paisesRepository.ObterPaisesResumo(pagina, tamanhoPagina);

  public Task<ResultadoPaginado<PaisResumoDto>> PesquisarPaises(string termo, int pagina = 1, int tamanhoPagina = 20)
      => _paisesRepository.PesquisarPaises(termo, pagina, tamanhoPagina);
}

