using Backend.Core.Common;
using Backend.Core.Features.Localizacao.DTOs;
using Backend.Core.Features.Localizacao.Entities;
using Backend.Core.Features.Localizacao.Repositories;
using Backend.Core.Features.Localizacao.Validators;
using FluentValidation;

namespace Backend.Core.Features.Localizacao.Services;

public sealed class EstadosService
{
  private readonly IEstadosRepository _estadosRepository;
  private readonly IPaisesRepository _paisesRepository;

  public EstadosService(IEstadosRepository estadosRepository, IPaisesRepository paisesRepository)
  {
    _estadosRepository = estadosRepository;
    _paisesRepository = paisesRepository;
  }

  public Task<ResultadoPaginado<Estados>> ObterEstados(int pagina = 1, int tamanhoDaPagina = 20)
      => _estadosRepository.ObterEstados(pagina, tamanhoDaPagina);

  public Task<Estados?> ObterEstadoPorId(int id)
      => _estadosRepository.ObterEstadoPorId(id);

  public async Task<Estados> CriarEstado(CreateEstadoDto dto)
  {
    new CreateEstadoDtoValidator().ValidateAndThrow(dto);

    var pais = await _paisesRepository.ObterPaisPorId(dto.PaisId);
    if (pais is null)
      throw new DomainException("País não encontrado.");

    return await _estadosRepository.CriarEstado(new Estados(dto.Estado, dto.Uf, pais));
  }

  public async Task<Estados> AtualizarEstado(int id, UpdateEstadoDto dto)
  {
    new UpdateEstadoDtoValidator().ValidateAndThrow(dto);

    var existente = await _estadosRepository.ObterEstadoPorId(id);
    if (existente is null)
      throw new DomainException("Estado não encontrado.");

    var pais = await _paisesRepository.ObterPaisPorId(dto.PaisId);
    if (pais is null)
      throw new DomainException("País não encontrado.");

    existente.Atualizar(dto.Estado, dto.Uf, pais);
    return await _estadosRepository.AtualizarEstado(id, existente);
  }

  public Task<bool> DeletarEstado(int id)
      => _estadosRepository.DeletarEstado(id);

  public Task<ResultadoPaginado<EstadoResumoDto>> ObterEstadosResumo(int pagina = 1, int tamanhoDaPagina = 20)
      => _estadosRepository.ObterEstadosResumo(pagina, tamanhoDaPagina);

  public Task<ResultadoPaginado<EstadoResumoDto>> PesquisarEstados(string termo, int pagina = 1, int tamanhoDaPagina = 20)
      => _estadosRepository.PesquisarEstados(termo, pagina, tamanhoDaPagina);
}

