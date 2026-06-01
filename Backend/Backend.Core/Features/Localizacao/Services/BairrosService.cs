using Backend.Core.Common;
using Backend.Core.Features.Localizacao.DTOs;
using Backend.Core.Features.Localizacao.Entities;
using Backend.Core.Features.Localizacao.Repositories;
using Backend.Core.Features.Localizacao.Validators;
using FluentValidation;

namespace Backend.Core.Features.Localizacao.Services;

public sealed class BairrosService
{
  private readonly IBairrosRepository _bairrosRepository;
  private readonly ICidadesRepository _cidadesRepository;

  public BairrosService(IBairrosRepository bairrosRepository, ICidadesRepository cidadesRepository)
  {
    _bairrosRepository = bairrosRepository;
    _cidadesRepository = cidadesRepository;
  }

  public Task<ResultadoPaginado<BairroResumoDto>> ObterBairros(string? search, int pagina = 1, int tamanhoDaPagina = 20)
      => _bairrosRepository.ObterBairros(search, pagina, tamanhoDaPagina);

  public Task<Bairros?> ObterBairroPorId(int id)
      => _bairrosRepository.ObterBairroPorId(id);

  public async Task<Resultado<Bairros>> CriarBairro(CreateBairroDto dto)
  {
    var validation = new CreateBairroDtoValidator().Validate(dto);
    if (!validation.IsValid)
      return Resultado<Bairros>.Falha(validation.ToResultadoErros());

    var cidade = await _cidadesRepository.ObterCidadePorId(dto.CidadeId);
    if (cidade is null)
      return Resultado<Bairros>.Falha(new ResultadoErro("CIDADE_NAO_ENCONTRADA", "Cidade não encontrada."));

    var entidadeResult = Bairros.Criar(dto.Bairro, cidade);
    if (!entidadeResult.Success)
      return entidadeResult;

    var criado = await _bairrosRepository.CriarBairro(entidadeResult.Data!);
    return Resultado<Bairros>.Sucesso(criado);
  }

  public async Task<Resultado<Bairros>> AtualizarBairro(int id, UpdateBairroDto dto)
  {
    var validation = new UpdateBairroDtoValidator().Validate(dto);
    if (!validation.IsValid)
      return Resultado<Bairros>.Falha(validation.ToResultadoErros());

    var existente = await _bairrosRepository.ObterBairroPorId(id);
    if (existente is null)
      return Resultado<Bairros>.Falha(new ResultadoErro("BAIRRO_NAO_ENCONTRADO", "Bairro não encontrado."));

    var cidade = await _cidadesRepository.ObterCidadePorId(dto.CidadeId);
    if (cidade is null)
      return Resultado<Bairros>.Falha(new ResultadoErro("CIDADE_NAO_ENCONTRADA", "Cidade não encontrada."));

    var updateResult = existente.AtualizarResultado(dto.Bairro, cidade);
    if (!updateResult.Success)
      return updateResult;

    var atualizado = await _bairrosRepository.AtualizarBairro(id, existente);
    return Resultado<Bairros>.Sucesso(atualizado);
  }

  public Task<bool> DeletarBairro(int id)
      => _bairrosRepository.DeletarBairro(id);
}

