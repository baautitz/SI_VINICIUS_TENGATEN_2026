using Backend.Core.Common.Extensions;
using Backend.Core.Common.Results;
using Backend.Core.Common;
using Backend.Core.Features.Catalogo.Commands;
using Backend.Core.Features.Catalogo.Entities;
using Backend.Core.Features.Catalogo.Repositories;
using Backend.Core.Features.Catalogo.Validators.Commands;
using FluentValidation;

namespace Backend.Core.Features.Catalogo.Services;

public sealed class CategoriasService : BaseService
{
    private readonly ICategoriasRepository _categoriasRepository;

    public CategoriasService(ICategoriasRepository categoriasRepository)
    {
        _categoriasRepository = categoriasRepository;
    }

    public Task<ResultadoPaginado<Categorias>> ObterCategorias(string? search, int pagina = 1, int tamanhoPagina = 20)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return _categoriasRepository.ObterCategorias(pagina, tamanhoPagina);
        }
        return _categoriasRepository.PesquisarCategorias(search, pagina, tamanhoPagina);
    }

    public Task<Categorias?> ObterCategoriaPorId(int id)
        => _categoriasRepository.ObterCategoriaPorId(id);

    public async Task<Resultado<Categorias>> CriarCategoria(CriarCategoriaCommand command)
    {
        var validation = new CriarCategoriaCommandValidator().Validate(command);
        if (!validation.IsValid)
            return Resultado<Categorias>.Falha(validation.ToResultadoErros());

        if (await _categoriasRepository.ExisteCategoria(command.Categoria))
            return Resultado<Categorias>.Falha(new ResultadoErro("DUPLICIDADE", "Já existe uma categoria com este nome.", "Categoria"));

        var entidade = new Categorias(command.Categoria, command.Descricao);

        return await ExecuteResultAsync(async () =>
        {
            var criado = await _categoriasRepository.CriarCategoria(entidade);
            return Resultado<Categorias>.Sucesso(criado);
        });
    }

    public async Task<Resultado<Categorias>> AtualizarCategoria(int id, AtualizarCategoriaCommand command)
    {
        var validation = new AtualizarCategoriaCommandValidator().Validate(command);
        if (!validation.IsValid)
            return Resultado<Categorias>.Falha(validation.ToResultadoErros());

        var existente = await _categoriasRepository.ObterCategoriaPorId(id);
        if (existente is null)
            return Resultado<Categorias>.Falha(new ResultadoErro("CATEGORIA_NAO_ENCONTRADA", "Categoria não encontrada."));

        if (await _categoriasRepository.ExisteCategoria(command.Categoria, id))
            return Resultado<Categorias>.Falha(new ResultadoErro("DUPLICIDADE", "Já existe outra categoria com este nome.", "Categoria"));

        existente.Atualizar(command.Categoria, command.Descricao);
        if (command.Ativo) existente.Ativar(); else existente.Desativar();

        return await ExecuteResultAsync(async () =>
        {
            var atualizado = await _categoriasRepository.AtualizarCategoria(id, existente);
            return Resultado<Categorias>.Sucesso(atualizado);
        });
    }

    public Task<bool> DeletarCategoria(int id)
        => _categoriasRepository.DeletarCategoria(id);
}
