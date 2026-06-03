using Backend.Core.Common.Extensions;
using Backend.Core.Common.Results;
using Backend.Core.Common;
using Backend.Core.Features.Catalogo.DTOs;
using Backend.Core.Features.Catalogo.Entities;
using Backend.Core.Features.Catalogo.Repositories;
using Backend.Core.Features.Catalogo.Validators;
using FluentValidation;

namespace Backend.Core.Features.Catalogo.Services;

public sealed class MarcasService : BaseService
{
    private readonly IMarcasRepository _marcasRepository;

    public MarcasService(IMarcasRepository marcasRepository)
    {
        _marcasRepository = marcasRepository;
    }

    public Task<ResultadoPaginado<MarcasResumo>> ObterMarcas(string? search, int pagina = 1, int tamanhoPagina = 20)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return _marcasRepository.ObterMarcasResumo(pagina, tamanhoPagina);
        }
        return _marcasRepository.PesquisarMarcas(search, pagina, tamanhoPagina);
    }

    public Task<Marcas?> ObterMarcaPorId(int id)
        => _marcasRepository.ObterMarcaPorId(id);

    public async Task<Resultado<Marcas>> CriarMarca(CreateMarcaDto dto)
    {
        var validation = new CreateMarcaDtoValidator().Validate(dto);
        if (!validation.IsValid)
            return Resultado<Marcas>.Falha(validation.ToResultadoErros());

        if (await _marcasRepository.ExisteMarca(dto.Marca))
            return Resultado<Marcas>.Falha(new ResultadoErro("DUPLICIDADE", "Já existe uma marca com este nome.", "Marca"));

        var entidade = new Marcas(dto.Marca, dto.Descricao);

        return await ExecuteResultAsync(async () =>
        {
            var criado = await _marcasRepository.CriarMarca(entidade);
            return Resultado<Marcas>.Sucesso(criado);
        });
    }

    public async Task<Resultado<Marcas>> AtualizarMarca(int id, UpdateMarcaDto dto)
    {
        var validation = new UpdateMarcaDtoValidator().Validate(dto);
        if (!validation.IsValid)
            return Resultado<Marcas>.Falha(validation.ToResultadoErros());

        var existente = await _marcasRepository.ObterMarcaPorId(id);
        if (existente is null)
            return Resultado<Marcas>.Falha(new ResultadoErro("MARCA_NAO_ENCONTRADA", "Marca não encontrada."));

        if (await _marcasRepository.ExisteMarca(dto.Marca, id))
            return Resultado<Marcas>.Falha(new ResultadoErro("DUPLICIDADE", "Já existe outra marca com este nome.", "Marca"));

        existente.Atualizar(dto.Marca, dto.Descricao);
        if (dto.Ativo) existente.Ativar(); else existente.Desativar();

        return await ExecuteResultAsync(async () =>
        {
            var atualizado = await _marcasRepository.AtualizarMarca(id, existente);
            return Resultado<Marcas>.Sucesso(atualizado);
        });
    }

    public Task<bool> DeletarMarca(int id)
        => _marcasRepository.DeletarMarca(id);
}
