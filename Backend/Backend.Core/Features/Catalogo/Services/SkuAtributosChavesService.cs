using Backend.Core.Common.Extensions;
using Backend.Core.Common.Results;
using Backend.Core.Common;
using Backend.Core.Features.Catalogo.DTOs;
using Backend.Core.Features.Catalogo.Entities;
using Backend.Core.Features.Catalogo.Repositories;
using Backend.Core.Features.Catalogo.Validators;
using FluentValidation;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Backend.Core.Features.Catalogo.Services;

public sealed class SkuAtributosChavesService : BaseService
{
    private readonly ISkuAtributosChavesRepository _atributosRepository;

    public SkuAtributosChavesService(ISkuAtributosChavesRepository atributosRepository)
    {
        _atributosRepository = atributosRepository;
    }

    public Task<ResultadoPaginado<SkuAtributosChavesResumo>> ObterAtributos(string? search, int pagina = 1, int tamanhoPagina = 20)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return _atributosRepository.ObterAtributosResumo(pagina, tamanhoPagina);
        }
        return _atributosRepository.PesquisarAtributos(search, pagina, tamanhoPagina);
    }

    public Task<SkuAtributosChaves?> ObterAtributoPorId(int id)
        => _atributosRepository.ObterAtributoPorId(id);

    public async Task<Resultado<SkuAtributosChaves>> CriarAtributo(CreateSkuAtributosChavesDto dto)
    {
        var validation = new CreateSkuAtributosChavesDtoValidator().Validate(dto);
        if (!validation.IsValid)
            return Resultado<SkuAtributosChaves>.Falha(validation.ToResultadoErros());

        if (await _atributosRepository.ExisteChave(dto.Chave))
            return Resultado<SkuAtributosChaves>.Falha(new ResultadoErro("DUPLICIDADE", "Já existe um atributo com este nome.", "Chave"));

        var entidade = new SkuAtributosChaves(dto.Chave);

        if (dto.Valores != null)
        {
            foreach (var valor in dto.Valores.Distinct())
            {
                if (!string.IsNullOrWhiteSpace(valor))
                {
                    entidade.AdicionarValor(new SkuAtributosValores(0, valor));
                }
            }
        }

        return await ExecuteResultAsync(async () =>
        {
            var criado = await _atributosRepository.CriarAtributo(entidade);
            return Resultado<SkuAtributosChaves>.Sucesso(criado);
        });
    }

    public async Task<Resultado<SkuAtributosChaves>> AtualizarAtributo(int id, UpdateSkuAtributosChavesDto dto)
    {
        var validation = new UpdateSkuAtributosChavesDtoValidator().Validate(dto);
        if (!validation.IsValid)
            return Resultado<SkuAtributosChaves>.Falha(validation.ToResultadoErros());

        var existente = await _atributosRepository.ObterAtributoPorId(id);
        if (existente is null)
            return Resultado<SkuAtributosChaves>.Falha(new ResultadoErro("ATRIBUTO_NAO_ENCONTRADO", "Atributo não encontrado."));

        if (await _atributosRepository.ExisteChave(dto.Chave, id))
            return Resultado<SkuAtributosChaves>.Falha(new ResultadoErro("DUPLICIDADE", "Já existe outro atributo com este nome.", "Chave"));

        existente.Atualizar(dto.Chave);
        existente.LimparValores();

        if (dto.Valores != null)
        {
            foreach (var valor in dto.Valores.Distinct())
            {
                if (!string.IsNullOrWhiteSpace(valor))
                {
                    existente.AdicionarValor(new SkuAtributosValores(id, valor));
                }
            }
        }

        return await ExecuteResultAsync(async () =>
        {
            var atualizado = await _atributosRepository.AtualizarAtributo(id, existente);
            return Resultado<SkuAtributosChaves>.Sucesso(atualizado);
        });
    }

    public Task<bool> DeletarAtributo(int id)
        => _atributosRepository.DeletarAtributo(id);
}
