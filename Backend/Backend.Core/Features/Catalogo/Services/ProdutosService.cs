using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend.Core.Common;
using Backend.Core.Common.Exceptions;
using Backend.Core.Common.Extensions;
using Backend.Core.Common.Interfaces;
using Backend.Core.Common.Results;
using Backend.Core.Features.Catalogo.DTOs;
using Backend.Core.Features.Catalogo.Entities;
using Backend.Core.Features.Catalogo.Repositories;
using Backend.Core.Features.Catalogo.Validators;
using FluentValidation;

namespace Backend.Core.Features.Catalogo.Services;

public sealed class ProdutosService : BaseService
{
    private readonly IProdutosRepository _produtosRepository;
    private readonly ISkusRepository _skusRepository;
    private readonly ICategoriasRepository _categoriasRepository;
    private readonly IMarcasRepository _marcasRepository;
    private readonly IUnidadesMedidaRepository _unidadesMedidaRepository;
    private readonly ISkuAtributosChavesRepository _atributosRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ProdutosService(
        IProdutosRepository produtosRepository,
        ISkusRepository skusRepository,
        ICategoriasRepository categoriasRepository,
        IMarcasRepository marcasRepository,
        IUnidadesMedidaRepository unidadesMedidaRepository,
        ISkuAtributosChavesRepository atributosRepository,
        IUnitOfWork unitOfWork)
    {
        _produtosRepository = produtosRepository;
        _skusRepository = skusRepository;
        _categoriasRepository = categoriasRepository;
        _marcasRepository = marcasRepository;
        _unidadesMedidaRepository = unidadesMedidaRepository;
        _atributosRepository = atributosRepository;
        _unitOfWork = unitOfWork;
    }

    public Task<ResultadoPaginado<ProdutosResumo>> ObterProdutos(string? search, int pagina = 1, int tamanhoPagina = 20)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return _produtosRepository.ObterProdutosResumo(pagina, tamanhoPagina);
        }
        return _produtosRepository.PesquisarProdutos(search, pagina, tamanhoPagina);
    }

    public Task<Produtos?> ObterProdutoPorId(int id)
        => _produtosRepository.ObterProdutoPorId(id);

    public async Task<Resultado<Produtos>> CriarProduto(CreateProdutoDto dto)
    {
        var validation = new CreateProdutoDtoValidator().Validate(dto);
        if (!validation.IsValid)
            return Resultado<Produtos>.Falha(validation.ToResultadoErros());

        if (await _produtosRepository.ExisteProduto(dto.Produto))
            return Resultado<Produtos>.Falha(new ResultadoErro("DUPLICIDADE", "Já existe um produto com este nome.", "Produto"));

        // Validate dependencies
        var categoria = await _categoriasRepository.ObterCategoriaPorId(dto.CategoriaId);
        if (categoria == null)
            return Resultado<Produtos>.Falha(new ResultadoErro("CATEGORIA_INEXISTENTE", "A categoria informada não existe.", "CategoriaId"));

        var marca = await _marcasRepository.ObterMarcaPorId(dto.MarcaId);
        if (marca == null)
            return Resultado<Produtos>.Falha(new ResultadoErro("MARCA_INEXISTENTE", "A marca informada não existe.", "MarcaId"));

        var unidadeMedida = await ObterUnidadeMedidaPorIdCompativel(dto.UnidadeMedidaId);
        if (unidadeMedida == null)
            return Resultado<Produtos>.Falha(new ResultadoErro("UNIDADE_MEDIDA_INEXISTENTE", "A unidade de medida informada não existe.", "UnidadeMedidaId"));

        var produto = new Produtos(dto.Produto, dto.Descricao, categoria, marca, unidadeMedida);
        if (!dto.Ativo)
            produto.Desativar();

        // Start transaction
        _unitOfWork.BeginTransaction();
        try
        {
            var criado = await _produtosRepository.CriarProduto(produto);
            int skuIndex = 1;

            foreach (var skuDto in dto.Skus)
            {
                var finalSkuCode = skuDto.Sku;
                if (string.IsNullOrWhiteSpace(finalSkuCode))
                {
                    finalSkuCode = $"{criado.Id}-{skuIndex}";
                }
                else if (finalSkuCode.StartsWith("0-") || finalSkuCode.StartsWith("novo-"))
                {
                    var parts = finalSkuCode.Split('-');
                    var suffix = parts.Last();
                    finalSkuCode = $"{criado.Id}-{suffix}";
                }
                skuIndex++;

                // Verify SKU uniqueness
                var existenteSku = await _skusRepository.ObterSkuPorSku(finalSkuCode);
                if (existenteSku != null)
                {
                    _unitOfWork.Rollback();
                    return Resultado<Produtos>.Falha(new ResultadoErro("SKU_DUPLICADO", $"O SKU '{finalSkuCode}' já está em uso por outro produto.", "Skus"));
                }

                var sku = new Skus(finalSkuCode, skuDto.Preco, skuDto.Estoque, skuDto.Ativo, skuDto.GtinEan);
                
                if (skuDto.AtributoValorIds != null && skuDto.AtributoValorIds.Any())
                {
                    var valoresAtributo = await _atributosRepository.ObterValoresPorIds(skuDto.AtributoValorIds);
                    sku.DefinirAtributos(valoresAtributo);
                }

                await _skusRepository.CriarSku(criado.Id, sku);
            }

            _unitOfWork.Commit();
            return Resultado<Produtos>.Sucesso(criado);
        }
        catch (Exception ex)
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    public async Task<Resultado<Produtos>> AtualizarProduto(int id, UpdateProdutoDto dto)
    {
        var validation = new UpdateProdutoDtoValidator().Validate(dto);
        if (!validation.IsValid)
            return Resultado<Produtos>.Falha(validation.ToResultadoErros());

        var existente = await _produtosRepository.ObterProdutoPorId(id);
        if (existente == null)
            return Resultado<Produtos>.Falha(new ResultadoErro("PRODUTO_NAO_ENCONTRADO", "Produto não encontrado."));

        if (await _produtosRepository.ExisteProduto(dto.Produto, id))
            return Resultado<Produtos>.Falha(new ResultadoErro("DUPLICIDADE", "Já existe outro produto com este nome.", "Produto"));

        // Validate dependencies
        var categoria = await _categoriasRepository.ObterCategoriaPorId(dto.CategoriaId);
        if (categoria == null)
            return Resultado<Produtos>.Falha(new ResultadoErro("CATEGORIA_INEXISTENTE", "A categoria informada não existe.", "CategoriaId"));

        var marca = await _marcasRepository.ObterMarcaPorId(dto.MarcaId);
        if (marca == null)
            return Resultado<Produtos>.Falha(new ResultadoErro("MARCA_INEXISTENTE", "A marca informada não existe.", "MarcaId"));

        var unidadeMedida = await ObterUnidadeMedidaPorIdCompativel(dto.UnidadeMedidaId);
        if (unidadeMedida == null)
            return Resultado<Produtos>.Falha(new ResultadoErro("UNIDADE_MEDIDA_INEXISTENTE", "A unidade de medida informada não existe.", "UnidadeMedidaId"));

        existente.Atualizar(dto.Produto, dto.Descricao, categoria, marca, unidadeMedida);
        if (dto.Ativo) existente.Ativar(); else existente.Desativar();

        // Start transaction
        _unitOfWork.BeginTransaction();
        try
        {
            // 1. Update the base product
            await _produtosRepository.AtualizarProduto(id, existente);

            // 2. Get current SKUs from database for this product
            var skusPaginado = await _skusRepository.ObterSkusPorProduto(id);
            var skusAtuais = skusPaginado.Itens.ToList();

            var skusNovos = dto.Skus;

            // 3. SKUs to delete: currently in db, but not in new list
            var codigosSkuNovos = skusNovos.Select(s => s.Sku.ToUpperInvariant()).ToHashSet();
            var skusParaDeletar = skusAtuais.Where(s => !codigosSkuNovos.Contains(s.Sku.ToUpperInvariant())).ToList();

            foreach (var skuParaDeletar in skusParaDeletar)
            {
                await _skusRepository.DeletarSku(skuParaDeletar.Sku);
            }

            // 4. Process new/updated SKUs
            foreach (var skuDto in skusNovos)
            {
                var skuExistenteDb = skusAtuais.FirstOrDefault(s => s.Sku.Equals(skuDto.Sku, StringComparison.OrdinalIgnoreCase));
                var skuEntity = new Skus(skuDto.Sku, skuDto.Preco, skuDto.Estoque, skuDto.Ativo, skuDto.GtinEan);
                
                if (skuDto.AtributoValorIds != null && skuDto.AtributoValorIds.Any())
                {
                    var valoresAtributo = await _atributosRepository.ObterValoresPorIds(skuDto.AtributoValorIds);
                    skuEntity.DefinirAtributos(valoresAtributo);
                }

                if (skuExistenteDb == null)
                {
                    // SKU is new, check absolute uniqueness across all products
                    var outroSkuExistente = await _skusRepository.ObterSkuPorSku(skuDto.Sku);
                    if (outroSkuExistente != null)
                    {
                        _unitOfWork.Rollback();
                        return Resultado<Produtos>.Falha(new ResultadoErro("SKU_DUPLICADO", $"O SKU '{skuDto.Sku}' já está em uso por outro produto.", "Skus"));
                    }

                    await _skusRepository.CriarSku(id, skuEntity);
                }
                else
                {
                    // SKU already exists for this product, update it
                    await _skusRepository.AtualizarSku(skuDto.Sku, skuEntity);
                }
            }

            _unitOfWork.Commit();
            
            // Reload updated complete product
            var atualizado = await _produtosRepository.ObterProdutoPorId(id);
            return Resultado<Produtos>.Sucesso(atualizado!);
        }
        catch (Exception ex)
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    public async Task<bool> DeletarProduto(int id)
    {
        // Start transaction for clean cascading if required, but repository deletes are single statements
        // Let's delete the SKUs and attributes relations first, then delete product.
        var produto = await _produtosRepository.ObterProdutoPorId(id);
        if (produto == null) return false;

        _unitOfWork.BeginTransaction();
        try
        {
            // Delete all SKUs for this product
            var skusResult = await _skusRepository.ObterSkusPorProduto(id);
            foreach (var sku in skusResult.Itens)
            {
                await _skusRepository.DeletarSku(sku.Sku);
            }

            // Delete product
            var deletado = await _produtosRepository.DeletarProduto(id);
            _unitOfWork.Commit();
            return deletado;
        }
        catch (Exception ex)
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    private async Task<UnidadesMedida?> ObterUnidadeMedidaPorIdCompativel(int id)
    {
        return await _unidadesMedidaRepository.ObterUnidadeMedidaPorId(id);
    }
}
