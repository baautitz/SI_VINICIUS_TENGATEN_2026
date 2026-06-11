using Backend.Core.Common.Results;
using Backend.Core.Common;
using Backend.Core.Features.Localizacao.Commands;
using Backend.Core.Features.Localizacao.Entities;
using Backend.Core.Features.Localizacao.Repositories;
using Backend.Core.Features.Localizacao.Validators.Commands;
using Backend.Core.Common.Extensions;
using FluentValidation;

namespace Backend.Core.Features.Localizacao.Services;

public sealed class CidadesService : BaseService
{
    private readonly ICidadesRepository _cidadesRepository;
    private readonly IEstadosRepository _estadosRepository;

    public CidadesService(ICidadesRepository cidadesRepository, IEstadosRepository estadosRepository)
    {
        _cidadesRepository = cidadesRepository;
        _estadosRepository = estadosRepository;
    }

    public Task<ResultadoPaginado<Cidades>> ObterCidades(string? search, int pagina = 1, int tamanhoPagina = 20)
    {
        if (string.IsNullOrWhiteSpace(search))
            return _cidadesRepository.ObterCidades(pagina, tamanhoPagina);
        return _cidadesRepository.PesquisarCidades(search, pagina, tamanhoPagina);
    }

    public Task<Cidades?> ObterCidadePorId(int id)
        => _cidadesRepository.ObterCidadePorId(id);

    public async Task<Resultado<Cidades>> CriarCidade(CriarCidadeCommand command)
    {
      var validation = new CriarCidadeCommandValidator().Validate(command);
      if (!validation.IsValid)
        return Resultado<Cidades>.Falha(validation.ToResultadoErros());

      var estado = await _estadosRepository.ObterEstadoPorId(command.EstadoId);
      if (estado is null)
        return Resultado<Cidades>.Falha(new ResultadoErro("ESTADO_NAO_ENCONTRADO", "Estado não encontrado.", "EstadoId"));

      if (await _cidadesRepository.ExisteCidade(command.Cidade, command.EstadoId))
        return Resultado<Cidades>.Falha(new ResultadoErro("DUPLICIDADE", "Já existe uma cidade com este nome neste estado.", "Cidade"));

      return await ExecuteResultAsync(async () =>
      {
        var result = Cidades.Criar(command.Cidade, command.Ddd, estado);
        if (!result.Success) return result;
        var criado = await _cidadesRepository.CriarCidade(result.Data!);
        return Resultado<Cidades>.Sucesso(criado);
      });
    }

    public async Task<Resultado<Cidades>> AtualizarCidade(int id, AtualizarCidadeCommand command)
    {
      var validation = new AtualizarCidadeCommandValidator().Validate(command);
      if (!validation.IsValid)
        return Resultado<Cidades>.Falha(validation.ToResultadoErros());

      var existente = await _cidadesRepository.ObterCidadePorId(id);
      if (existente is null)
        return Resultado<Cidades>.Falha(new ResultadoErro("CIDADE_NAO_ENCONTRADA", "Cidade não encontrada."));

      var estado = await _estadosRepository.ObterEstadoPorId(command.EstadoId);
      if (estado is null)
        return Resultado<Cidades>.Falha(new ResultadoErro("ESTADO_NAO_ENCONTRADO", "Estado não encontrado.", "EstadoId"));

      if (await _cidadesRepository.ExisteCidade(command.Cidade, command.EstadoId, id))
        return Resultado<Cidades>.Falha(new ResultadoErro("DUPLICIDADE", "Já existe outra cidade com este nome neste estado.", "Cidade"));

      return await ExecuteResultAsync(async () =>
      {
          var result = existente.AtualizarResultado(command.Cidade, command.Ddd, estado);
          if (!result.Success) return result;
          var atualizado = await _cidadesRepository.AtualizarCidade(id, existente);
          return Resultado<Cidades>.Sucesso(atualizado);
      });
    }
    public Task<bool> DeletarCidade(int id)
        => _cidadesRepository.DeletarCidade(id);
}
