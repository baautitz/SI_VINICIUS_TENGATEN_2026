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

  public Task<ResultadoPaginado<Bairros>> ObterBairros(int pagina = 1, int tamanhoDaPagina = 20)
      => _bairrosRepository.ObterBairros(pagina, tamanhoDaPagina);

  public Task<Bairros?> ObterBairroPorId(int id)
      => _bairrosRepository.ObterBairroPorId(id);

  public async Task<Bairros> CriarBairro(CreateBairroDto dto)
  {
    new CreateBairroDtoValidator().ValidateAndThrow(dto);

    var cidade = await _cidadesRepository.ObterCidadePorId(dto.CidadeId);
    if (cidade is null)
      throw new DomainException("Cidade não encontrada.");

    return await _bairrosRepository.CriarBairro(new Bairros(dto.Bairro, cidade));
  }

  public async Task<Bairros> AtualizarBairro(int id, UpdateBairroDto dto)
  {
    new UpdateBairroDtoValidator().ValidateAndThrow(dto);

    var existente = await _bairrosRepository.ObterBairroPorId(id);
    if (existente is null)
      throw new DomainException("Bairro não encontrado.");

    var cidade = await _cidadesRepository.ObterCidadePorId(dto.CidadeId);
    if (cidade is null)
      throw new DomainException("Cidade não encontrada.");

    existente.Atualizar(dto.Bairro, cidade);
    return await _bairrosRepository.AtualizarBairro(id, existente);
  }

  public Task<bool> DeletarBairro(int id)
      => _bairrosRepository.DeletarBairro(id);

  public Task<ResultadoPaginado<BairroResumoDto>> ObterBairrosResumo(int pagina = 1, int tamanhoDaPagina = 20)
      => _bairrosRepository.ObterBairrosResumo(pagina, tamanhoDaPagina);

  public Task<ResultadoPaginado<BairroResumoDto>> PesquisarBairros(string termo, int pagina = 1, int tamanhoDaPagina = 20)
      => _bairrosRepository.PesquisarBairros(termo, pagina, tamanhoDaPagina);
}

