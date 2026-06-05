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

        var categoria = await _categoriasRepository.ObterCategoriaPorId(dto.CategoriaId!.Value);
        if (categoria == null)
            return Resultado<Produtos>.Falha(new ResultadoErro("CATEGORIA_INEXISTENTE", "A categoria informada não existe.", "CategoriaId"));

        var marca = await _marcasRepository.ObterMarcaPorId(dto.MarcaId!.Value);
        if (marca == null)
            return Resultado<Produtos>.Falha(new ResultadoErro("MARCA_INEXISTENTE", "A marca informada não existe.", "MarcaId"));

        var unidadeMedida = await ObterUnidadeMedidaPorIdCompativel(dto.UnidadeMedidaId!.Value);
        if (unidadeMedida == null)
            return Resultado<Produtos>.Falha(new ResultadoErro("UNIDADE_MEDIDA_INEXISTENTE", "A unidade de medida informada não existe.", "UnidadeMedidaId"));

        var produto = new Produtos(dto.Produto, dto.Descricao, categoria, marca, unidadeMedida);
        if (!dto.Ativo)
            produto.Desativar();

        _unitOfWork.BeginTransaction();
        try
        {
            var criado = await _produtosRepository.CriarProduto(produto);
            var assignedSkuCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int skuIndex = 1;

            foreach (var skuDto in dto.Skus)
            {
                var finalSkuCode = skuDto.Sku;
                if (string.IsNullOrWhiteSpace(finalSkuCode))
                {
                    while (true)
                    {
                        var candidate = $"{criado.Id}-{skuIndex}";
                        if (!assignedSkuCodes.Contains(candidate))
                        {
                            finalSkuCode = candidate;
                            break;
                        }
                        skuIndex++;
                    }
                }
                assignedSkuCodes.Add(finalSkuCode);

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
        catch
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

        var categoria = await _categoriasRepository.ObterCategoriaPorId(dto.CategoriaId!.Value);
        if (categoria == null)
            return Resultado<Produtos>.Falha(new ResultadoErro("CATEGORIA_INEXISTENTE", "A categoria informada não existe.", "CategoriaId"));

        var marca = await _marcasRepository.ObterMarcaPorId(dto.MarcaId!.Value);
        if (marca == null)
            return Resultado<Produtos>.Falha(new ResultadoErro("MARCA_INEXISTENTE", "A marca informada não existe.", "MarcaId"));

        var unidadeMedida = await ObterUnidadeMedidaPorIdCompativel(dto.UnidadeMedidaId!.Value);
        if (unidadeMedida == null)
            return Resultado<Produtos>.Falha(new ResultadoErro("UNIDADE_MEDIDA_INEXISTENTE", "A unidade de medida informada não existe.", "UnidadeMedidaId"));

        existente.Atualizar(dto.Produto, dto.Descricao, categoria, marca, unidadeMedida);
        if (dto.Ativo) existente.Ativar(); else existente.Desativar();

        _unitOfWork.BeginTransaction();
        try
        {
            await _produtosRepository.AtualizarProduto(id, existente);

            var skusPaginado = await _skusRepository.ObterSkusPorProduto(id);
            var skusAtuais = skusPaginado.Itens.ToList();

            var skusNovos = dto.Skus;

            var codigosSkuNovos = skusNovos
                .Where(s => !string.IsNullOrWhiteSpace(s.Sku))
                .Select(s => s.Sku.ToUpperInvariant())
                .ToHashSet();
            var skusParaDeletar = skusAtuais.Where(s => !codigosSkuNovos.Contains(s.Sku.ToUpperInvariant())).ToList();

            foreach (var skuParaDeletar in skusParaDeletar)
            {
                await _skusRepository.DeletarSku(skuParaDeletar.Sku);
                skusAtuais.Remove(skuParaDeletar);
            }

            var assignedSkuCodes = skusAtuais.Select(s => s.Sku).ToHashSet(StringComparer.OrdinalIgnoreCase);
            int skuIndex = 1;
            foreach (var skuDto in skusNovos)
            {
                var finalSkuCode = skuDto.Sku;
                if (string.IsNullOrWhiteSpace(finalSkuCode))
                {
                    while (true)
                    {
                        var candidate = $"{id}-{skuIndex}";
                        if (!assignedSkuCodes.Contains(candidate))
                        {
                            finalSkuCode = candidate;
                            break;
                        }
                        skuIndex++;
                    }
                }
                assignedSkuCodes.Add(finalSkuCode);

                var skuExistenteDb = skusAtuais.FirstOrDefault(s => s.Sku.Equals(finalSkuCode, StringComparison.OrdinalIgnoreCase));
                var skuEntity = new Skus(finalSkuCode, skuDto.Preco, skuDto.Estoque, skuDto.Ativo, skuDto.GtinEan);
                
                if (skuDto.AtributoValorIds != null && skuDto.AtributoValorIds.Any())
                {
                    var valoresAtributo = await _atributosRepository.ObterValoresPorIds(skuDto.AtributoValorIds);
                    skuEntity.DefinirAtributos(valoresAtributo);
                }

                if (skuExistenteDb == null)
                {
                    var outroSkuExistente = await _skusRepository.ObterSkuPorSku(finalSkuCode);
                    if (outroSkuExistente != null)
                    {
                        _unitOfWork.Rollback();
                        return Resultado<Produtos>.Falha(new ResultadoErro("SKU_DUPLICADO", $"O SKU '{finalSkuCode}' já está em uso por outro produto.", "Skus"));
                    }

                    await _skusRepository.CriarSku(id, skuEntity);
                }
                else
                {
                    await _skusRepository.AtualizarSku(finalSkuCode, skuEntity);
                }
            }

            _unitOfWork.Commit();
            
            var atualizado = await _produtosRepository.ObterProdutoPorId(id);
            return Resultado<Produtos>.Sucesso(atualizado!);
        }
        catch
        {
            _unitOfWork.Rollback();
            throw;
        }
    }

    public async Task<bool> DeletarProduto(int id)
    {
        var produto = await _produtosRepository.ObterProdutoPorId(id);
        if (produto == null) return false;

        _unitOfWork.BeginTransaction();
        try
        {
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
        catch
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
