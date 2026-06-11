using Backend.Core.Common.Extensions;
using Backend.Core.Common.Results;
using Backend.Core.Common;
using Backend.Core.Features.Localizacao.Commands;
using Backend.Core.Features.Localizacao.Entities;
using Backend.Core.Features.Localizacao.Repositories;
using Backend.Core.Features.Localizacao.Validators.Commands;
using FluentValidation;

namespace Backend.Core.Features.Localizacao.Services;

public sealed class PaisesService : BaseService
{
    private readonly IPaisesRepository _paisesRepository;

    public PaisesService(IPaisesRepository paisesRepository)
    {
        _paisesRepository = paisesRepository;
    }

    public Task<ResultadoPaginado<Paises>> ObterPaises(string? search, int pagina = 1, int tamanhoPagina = 20)
    {
        if (string.IsNullOrWhiteSpace(search))
            return _paisesRepository.ObterPaises(pagina, tamanhoPagina);
        return _paisesRepository.PesquisarPaises(search, pagina, tamanhoPagina);
    }

    public Task<Paises?> ObterPaisPorId(int id)
        => _paisesRepository.ObterPaisPorId(id);

    public async Task<Resultado<Paises>> CriarPais(CriarPaisCommand command)
    {
        var validation = new CriarPaisCommandValidator().Validate(command);
        if (!validation.IsValid)
            return Resultado<Paises>.Falha(validation.ToResultadoErros());

        var entidadeResult = Paises.Criar(command.Ddi, command.CodigoIsoPais, command.CodigoIsoMoeda, command.SimboloMoeda, command.Pais);
        if (!entidadeResult.Success)
            return entidadeResult;

        if (await _paisesRepository.ExistePais(command.CodigoIsoPais, command.Pais))
            return Resultado<Paises>.Falha(new ResultadoErro("DUPLICIDADE", "Já existe um país com este nome ou código ISO.", "Pais"));

        return await ExecuteResultAsync(async () =>
        {
            var criado = await _paisesRepository.CriarPais(entidadeResult.Data!);
            return Resultado<Paises>.Sucesso(criado);
        });
    }

    public async Task<Resultado<Paises>> AtualizarPais(int id, AtualizarPaisCommand command)
    {
        var validation = new AtualizarPaisCommandValidator().Validate(command);
        if (!validation.IsValid)
            return Resultado<Paises>.Falha(validation.ToResultadoErros());

        var existente = await _paisesRepository.ObterPaisPorId(id);
        if (existente is null)
            return Resultado<Paises>.Falha(new ResultadoErro("PAIS_NAO_ENCONTRADO", "País não encontrado."));

        var updateResult = existente.AtualizarResultado(command.Ddi, command.CodigoIsoPais, command.CodigoIsoMoeda, command.SimboloMoeda, command.Pais);
        if (!updateResult.Success)
            return updateResult;

        if (await _paisesRepository.ExistePais(command.CodigoIsoPais, command.Pais, id))
            return Resultado<Paises>.Falha(new ResultadoErro("DUPLICIDADE", "Já existe outro país com este nome ou código ISO.", "Pais"));

        return await ExecuteResultAsync(async () =>
        {
            var atualizado = await _paisesRepository.AtualizarPais(id, existente);
            return Resultado<Paises>.Sucesso(atualizado);
        });
    }

    public Task<bool> DeletarPais(int id)
        => _paisesRepository.DeletarPais(id);
}
