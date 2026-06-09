using Backend.Core.Common.Extensions;
using Backend.Core.Common.Results;
using Backend.Core.Common;
using Backend.Core.Features.Catalogo.Commands;
using Backend.Core.Features.Catalogo.Entities;
using Backend.Core.Features.Catalogo.Repositories;
using Backend.Core.Features.Catalogo.Validators.Commands;
using FluentValidation;

namespace Backend.Core.Features.Catalogo.Services;

public sealed class MarcasService : BaseService
{
    private readonly IMarcasRepository _marcasRepository;

    public MarcasService(IMarcasRepository marcasRepository)
    {
        _marcasRepository = marcasRepository;
    }

    public Task<ResultadoPaginado<Marcas>> ObterMarcas(string? search, int pagina = 1, int tamanhoPagina = 20)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return _marcasRepository.ObterMarcas(pagina, tamanhoPagina);
        }
        return _marcasRepository.PesquisarMarcas(search, pagina, tamanhoPagina);
    }

    public Task<Marcas?> ObterMarcaPorId(int id)
        => _marcasRepository.ObterMarcaPorId(id);

    public async Task<Resultado<Marcas>> CriarMarca(CriarMarcaCommand command)
    {
        var validation = new CriarMarcaCommandValidator().Validate(command);
        if (!validation.IsValid)
            return Resultado<Marcas>.Falha(validation.ToResultadoErros());

        if (await _marcasRepository.ExisteMarca(command.Marca))
            return Resultado<Marcas>.Falha(new ResultadoErro("DUPLICIDADE", "Já existe uma marca com este nome.", "Marca"));

        var entidade = new Marcas(command.Marca, command.Descricao);

        return await ExecuteResultAsync(async () =>
        {
            var criado = await _marcasRepository.CriarMarca(entidade);
            return Resultado<Marcas>.Sucesso(criado);
        });
    }

    public async Task<Resultado<Marcas>> AtualizarMarca(int id, AtualizarMarcaCommand command)
    {
        var validation = new AtualizarMarcaCommandValidator().Validate(command);
        if (!validation.IsValid)
            return Resultado<Marcas>.Falha(validation.ToResultadoErros());

        var existente = await _marcasRepository.ObterMarcaPorId(id);
        if (existente is null)
            return Resultado<Marcas>.Falha(new ResultadoErro("MARCA_NAO_ENCONTRADA", "Marca não encontrada."));

        if (await _marcasRepository.ExisteMarca(command.Marca, id))
            return Resultado<Marcas>.Falha(new ResultadoErro("DUPLICIDADE", "Já existe outra marca com este nome.", "Marca"));

        existente.Atualizar(command.Marca, command.Descricao);
        if (command.Ativo) existente.Ativar(); else existente.Desativar();

        return await ExecuteResultAsync(async () =>
        {
            var atualizado = await _marcasRepository.AtualizarMarca(id, existente);
            return Resultado<Marcas>.Sucesso(atualizado);
        });
    }

    public Task<bool> DeletarMarca(int id)
        => _marcasRepository.DeletarMarca(id);
}
