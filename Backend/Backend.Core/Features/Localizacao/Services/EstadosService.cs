using Backend.Core.Common.Results;
using Backend.Core.Common;
using Backend.Core.Features.Localizacao.Commands;
using Backend.Core.Features.Localizacao.Entities;
using Backend.Core.Features.Localizacao.Repositories;
using Backend.Core.Features.Localizacao.Validators.Commands;
using Backend.Core.Common.Extensions;
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

    public Task<ResultadoPaginado<Estados>> ObterEstados(string? search, int pagina = 1, int tamanhoPagina = 20)
    {
        if (string.IsNullOrWhiteSpace(search))
            return _estadosRepository.ObterEstados(pagina, tamanhoPagina);
        return _estadosRepository.PesquisarEstados(search, pagina, tamanhoPagina);
    }

    public Task<Estados?> ObterEstadoPorId(int id)
        => _estadosRepository.ObterEstadoPorId(id);

    public async Task<Resultado<Estados>> CriarEstado(CriarEstadoCommand command)
    {
        var validation = new CriarEstadoCommandValidator().Validate(command);
        if (!validation.IsValid)
            return Resultado<Estados>.Falha(validation.ToResultadoErros());

        var pais = await _paisesRepository.ObterPaisPorId(command.PaisId);
        if (pais is null)
            return Resultado<Estados>.Falha(new ResultadoErro("PAIS_NAO_ENCONTRADO", "País não encontrado.", "PaisId"));

        if (await _estadosRepository.ExisteEstado(command.Uf, command.PaisId))
            return Resultado<Estados>.Falha(new ResultadoErro("DUPLICIDADE", "Já existe um estado com esta UF neste país.", "Uf"));

        return await ExecuteResultAsync(async () =>
        {
          var result = Estados.Criar(command.Estado, command.Uf, pais);
          if (!result.Success) return result;
          var criado = await _estadosRepository.CriarEstado(result.Data!);
          return Resultado<Estados>.Sucesso(criado);
        });

    }

    public async Task<Resultado<Estados>> AtualizarEstado(int id, AtualizarEstadoCommand command)
    {
        var validation = new AtualizarEstadoCommandValidator().Validate(command);
        if (!validation.IsValid)
            return Resultado<Estados>.Falha(validation.ToResultadoErros());

        var existente = await _estadosRepository.ObterEstadoPorId(id);
        if (existente is null)
            return Resultado<Estados>.Falha(new ResultadoErro("ESTADO_NAO_ENCONTRADO", "Estado não encontrado."));

        var pais = await _paisesRepository.ObterPaisPorId(command.PaisId);
        if (pais is null)
            return Resultado<Estados>.Falha(new ResultadoErro("PAIS_NAO_ENCONTRADO", "País não encontrado.", "PaisId"));

        if (await _estadosRepository.ExisteEstado(command.Uf, command.PaisId, id))
            return Resultado<Estados>.Falha(new ResultadoErro("DUPLICIDADE", "Já existe outro estado com esta UF neste país.", "Uf"));

        return await ExecuteResultAsync(async () =>
        {
            existente.AtualizarResultado(command.Estado, command.Uf, pais);
            var atualizado = await _estadosRepository.AtualizarEstado(id, existente);
            return Resultado<Estados>.Sucesso(atualizado);
        });
    }

    public Task<bool> DeletarEstado(int id)
        => _estadosRepository.DeletarEstado(id);
}
