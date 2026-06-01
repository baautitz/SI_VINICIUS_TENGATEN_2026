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

  public Task<ResultadoPaginado<CidadeResumoDto>> ObterCidades(string? search, int pagina = 1, int tamanhoDaPagina = 20)
      => _cidadesRepository.ObterCidades(search, pagina, tamanhoDaPagina);

  public Task<Cidades?> ObterCidadePorId(int id)
      => _cidadesRepository.ObterCidadePorId(id);

  public async Task<Resultado<Cidades>> CriarCidade(CreateCidadeDto dto)
  {
    var validation = new CreateCidadeDtoValidator().Validate(dto);
    if (!validation.IsValid)
      return Resultado<Cidades>.Falha(validation.ToResultadoErros());

    var estado = await _estadosRepository.ObterEstadoPorId(dto.EstadoId);
    if (estado is null)
      return Resultado<Cidades>.Falha(new ResultadoErro("ESTADO_NAO_ENCONTRADO", "Estado não encontrado."));

    var entidadeResult = Cidades.Criar(dto.Cidade, dto.Ddd, estado);
    if (!entidadeResult.Success)
      return entidadeResult;

    var criado = await _cidadesRepository.CriarCidade(entidadeResult.Data!);
    return Resultado<Cidades>.Sucesso(criado);
  }

  public async Task<Resultado<Cidades>> AtualizarCidade(int id, UpdateCidadeDto dto)
  {
    var validation = new UpdateCidadeDtoValidator().Validate(dto);
    if (!validation.IsValid)
      return Resultado<Cidades>.Falha(validation.ToResultadoErros());

    var existente = await _cidadesRepository.ObterCidadePorId(id);
    if (existente is null)
      return Resultado<Cidades>.Falha(new ResultadoErro("CIDADE_NAO_ENCONTRADA", "Cidade não encontrada."));

    var estado = await _estadosRepository.ObterEstadoPorId(dto.EstadoId);
    if (estado is null)
      return Resultado<Cidades>.Falha(new ResultadoErro("ESTADO_NAO_ENCONTRADO", "Estado não encontrado."));

    var updateResult = existente.AtualizarResultado(dto.Cidade, dto.Ddd, estado);
    if (!updateResult.Success)
      return updateResult;

    var atualizado = await _cidadesRepository.AtualizarCidade(id, existente);
    return Resultado<Cidades>.Sucesso(atualizado);
  }

  public Task<bool> DeletarCidade(int id)
      => _cidadesRepository.DeletarCidade(id);
}

