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

  public Task<Paises> CriarPais(CreatePaisDto dto)
  {
    new CreatePaisDtoValidator().ValidateAndThrow(dto);
    return _paisesRepository.CriarPais(new Paises(dto.Ddi, dto.SiglaIso, dto.Moeda, dto.SimboloMoeda, dto.Pais));
  }

  public async Task<Paises> AtualizarPais(int id, UpdatePaisDto dto)
  {
    new UpdatePaisDtoValidator().ValidateAndThrow(dto);

    var existente = await _paisesRepository.ObterPaisPorId(id);
    if (existente is null)
      throw new DomainException("País não encontrado.");

    existente.Atualizar(dto.Ddi, dto.SiglaIso, dto.Moeda, dto.SimboloMoeda, dto.Pais);
    return await _paisesRepository.AtualizarPais(id, existente);
  }

  public Task<bool> DeletarPais(int id)
      => _paisesRepository.DeletarPais(id);

  public Task<ResultadoPaginado<PaisResumoDto>> ObterPaisesResumo(int pagina = 1, int tamanhoPagina = 20)
      => _paisesRepository.ObterPaisesResumo(pagina, tamanhoPagina);

  public Task<ResultadoPaginado<PaisResumoDto>> PesquisarPaises(string termo, int pagina = 1, int tamanhoPagina = 20)
      => _paisesRepository.PesquisarPaises(termo, pagina, tamanhoPagina);
}

