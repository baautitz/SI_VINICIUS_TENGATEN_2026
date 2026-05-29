using Backend.Core.Common;
using Backend.Core.Features.Localizacao.DTOs;
using Backend.Core.Features.Localizacao.Entities;
using Backend.Core.Features.Localizacao.Repositories;
using Backend.Core.Features.Localizacao.Validators;
using FluentValidation;

namespace Backend.Core.Features.Localizacao.Services;

public sealed class CidadesService
{
  private readonly ICidadesRepository _cidadesRepository;
  private readonly IEstadosRepository _estadosRepository;

  public CidadesService(ICidadesRepository cidadesRepository, IEstadosRepository estadosRepository)
  {
    _cidadesRepository = cidadesRepository;
    _estadosRepository = estadosRepository;
  }

  public Task<ResultadoPaginado<Cidades>> ObterCidades(int pagina = 1, int tamanhoDaPagina = 20)
      => _cidadesRepository.ObterCidades(pagina, tamanhoDaPagina);

  public Task<Cidades?> ObterCidadePorId(int id)
      => _cidadesRepository.ObterCidadePorId(id);

  public async Task<Cidades> CriarCidade(CreateCidadeDto dto)
  {
    new CreateCidadeDtoValidator().ValidateAndThrow(dto);

    var estado = await _estadosRepository.ObterEstadoPorId(dto.EstadoId);
    if (estado is null)
      throw new DomainException("Estado não encontrado.");

    return await _cidadesRepository.CriarCidade(new Cidades(dto.Cidade, dto.Ddd, estado));
  }

  public async Task<Cidades> AtualizarCidade(int id, UpdateCidadeDto dto)
  {
    new UpdateCidadeDtoValidator().ValidateAndThrow(dto);

    var existente = await _cidadesRepository.ObterCidadePorId(id);
    if (existente is null)
      throw new DomainException("Cidade não encontrada.");

    var estado = await _estadosRepository.ObterEstadoPorId(dto.EstadoId);
    if (estado is null)
      throw new DomainException("Estado não encontrado.");

    existente.Atualizar(dto.Cidade, dto.Ddd, estado);
    return await _cidadesRepository.AtualizarCidade(id, existente);
  }

  public Task<bool> DeletarCidade(int id)
      => _cidadesRepository.DeletarCidade(id);

  public Task<ResultadoPaginado<CidadeResumoDto>> ObterCidadesResumo(int pagina = 1, int tamanhoDaPagina = 20)
      => _cidadesRepository.ObterCidadesResumo(pagina, tamanhoDaPagina);

  public Task<ResultadoPaginado<CidadeResumoDto>> PesquisarCidades(string termo, int pagina = 1, int tamanhoDaPagina = 20)
      => _cidadesRepository.PesquisarCidades(termo, pagina, tamanhoDaPagina);
}

