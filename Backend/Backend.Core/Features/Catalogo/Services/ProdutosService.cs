using Backend.Core.Common.Extensions;
using Backend.Core.Common.Results;
using Backend.Core.Common;
using Backend.Core.Common.Interfaces;
using Backend.Core.Features.Catalogo.Commands;
using Backend.Core.Features.Catalogo.Entities;
using Backend.Core.Features.Catalogo.Repositories;
using Backend.Core.Features.Catalogo.Validators.Commands;
using FluentValidation;

namespace Backend.Core.Features.Catalogo.Services;

public sealed class ProdutosService : BaseService
{
    private readonly IProdutosRepository _produtosRepository;
    private readonly ICategoriasRepository _categoriasRepository;
    private readonly IMarcasRepository _marcasRepository;
    private readonly IUnidadesMedidaRepository _unidadesMedidaRepository;
    private readonly ISkuAtributosChavesRepository _atributosRepository;
    private readonly ISkusRepository _skusRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ProdutosService(
        IProdutosRepository produtosRepository,
        ICategoriasRepository categoriasRepository,
        IMarcasRepository marcasRepository,
        IUnidadesMedidaRepository unidadesMedidaRepository,
        ISkuAtributosChavesRepository atributosRepository,
        ISkusRepository skusRepository,
        IUnitOfWork unitOfWork)
    {
        _produtosRepository = produtosRepository;
        _categoriasRepository = categoriasRepository;
        _marcasRepository = marcasRepository;
        _unidadesMedidaRepository = unidadesMedidaRepository;
        _atributosRepository = atributosRepository;
        _skusRepository = skusRepository;
        _unitOfWork = unitOfWork;
    }

    public Task<ResultadoPaginado<Produtos>> ObterProdutos(string? search, int pagina = 1, int tamanhoPagina = 20, int? categoriaId = null, int? marcaId = null, int? unidadeMedidaId = null)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return _produtosRepository.ObterProdutos(pagina, tamanhoPagina, categoriaId, marcaId, unidadeMedidaId);
        }
        return _produtosRepository.PesquisarProdutos(search, pagina, tamanhoPagina, categoriaId, marcaId, unidadeMedidaId);
    }

    public Task<Produtos?> ObterProdutoPorId(int id)
        => _produtosRepository.ObterProdutoPorId(id);

    public Task<Produtos?> ObterProdutoPorSku(string sku)
        => _produtosRepository.ObterProdutoPorSku(sku);

    public async Task<Resultado<Produtos>> CriarProduto(CriarProdutoCommand command)
    {
        var validation = new CriarProdutoCommandValidator().Validate(command);
        if (!validation.IsValid)
            return Resultado<Produtos>.Falha(validation.ToResultadoErros());

        if (await _produtosRepository.ExisteProduto(command.Produto))
            return Resultado<Produtos>.Falha(new ResultadoErro("DUPLICIDADE", "Já existe um produto com este nome.", "Produto"));

        var categoria = await _categoriasRepository.ObterCategoriaPorId(command.CategoriaId);
        if (categoria == null) return Resultado<Produtos>.Falha(new ResultadoErro("CATEGORIA_INEXISTENTE", "Categoria não encontrada.", "CategoriaId"));

        var marca = await _marcasRepository.ObterMarcaPorId(command.MarcaId);
        if (marca == null) return Resultado<Produtos>.Falha(new ResultadoErro("MARCA_INEXISTENTE", "Marca não encontrada.", "MarcaId"));

        var unidadeMedida = await _unidadesMedidaRepository.ObterUnidadeMedidaPorId(command.UnidadeMedidaId);
        if (unidadeMedida == null) return Resultado<Produtos>.Falha(new ResultadoErro("UNIDADE_MEDIDA_INEXISTENTE", "Unidade de medida não encontrada.", "UnidadeMedidaId"));

        var produto = new Produtos(command.Produto, command.Descricao, categoria, marca, unidadeMedida);
        if (!command.Ativo) produto.Desativar();

        return await ExecuteResultAsync(async () =>
        {
            var criado = await _produtosRepository.CriarProduto(produto);
            
            var skuIndex = 1;
            foreach (var skuCommand in command.Skus)
            {
                var skuCode = string.IsNullOrWhiteSpace(skuCommand.Sku) 
                    ? $"{criado.Id}{skuIndex++}" 
                    : skuCommand.Sku!;

                var sku = new Skus(skuCode, skuCommand.Preco, 0, skuCommand.Ativo, skuCommand.GtinEan);
                if (skuCommand.AtributoValorIds != null && skuCommand.AtributoValorIds.Any())
                {
                    var valoresAtributo = await _atributosRepository.ObterValoresPorIds(skuCommand.AtributoValorIds);
                    foreach (var valor in valoresAtributo) sku.AdicionarAtributo(valor);
                }
                
                await _skusRepository.CriarSku(criado.Id, sku);
                criado.AdicionarSku(sku);
            }

            return Resultado<Produtos>.Sucesso(criado);
        });
    }

    public async Task<Resultado<Produtos>> AtualizarProduto(int id, AtualizarProdutoCommand command)
    {
        var validation = new AtualizarProdutoCommandValidator().Validate(command);
        if (!validation.IsValid)
            return Resultado<Produtos>.Falha(validation.ToResultadoErros());

        var existente = await _produtosRepository.ObterProdutoPorId(id);
        if (existente is null)
            return Resultado<Produtos>.Falha(new ResultadoErro("PRODUTO_NAO_ENCONTRADO", "Produto não encontrado."));

        if (await _produtosRepository.ExisteProduto(command.Produto, id))
            return Resultado<Produtos>.Falha(new ResultadoErro("DUPLICIDADE", "Já existe outro produto com este nome.", "Produto"));

        var categoria = await _categoriasRepository.ObterCategoriaPorId(command.CategoriaId);
        if (categoria == null) return Resultado<Produtos>.Falha(new ResultadoErro("CATEGORIA_INEXISTENTE", "Categoria não encontrada.", "CategoriaId"));

        var marca = await _marcasRepository.ObterMarcaPorId(command.MarcaId);
        if (marca == null) return Resultado<Produtos>.Falha(new ResultadoErro("MARCA_INEXISTENTE", "Marca não encontrada.", "MarcaId"));

        var unidadeMedida = await _unidadesMedidaRepository.ObterUnidadeMedidaPorId(command.UnidadeMedidaId);
        if (unidadeMedida == null) return Resultado<Produtos>.Falha(new ResultadoErro("UNIDADE_MEDIDA_INEXISTENTE", "Unidade de medida não encontrada.", "UnidadeMedidaId"));

        return await ExecuteResultAsync(async () =>
        {
            try
            {
                _unitOfWork.BeginTransaction();

                var skusAtuais = await _skusRepository.ObterSkusPorProduto(id);
                var skusParaManter = command.Skus
                    .Where(s => !string.IsNullOrWhiteSpace(s.Sku))
                    .Select(s => s.Sku!)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var skusParaExcluir = skusAtuais.Itens
                    .Where(sku => !skusParaManter.Contains(sku.Sku))
                    .Select(sku => sku.Sku)
                    .ToArray();

                var skusComMovimentacoes = await _skusRepository
                    .ObterSkusComMovimentacoesEstoque(skusParaExcluir);

                if (skusComMovimentacoes.Count > 0)
                {
                    _unitOfWork.Rollback();

                    return Resultado<Produtos>.Falha(new ResultadoErro(
                        "SKU_COM_MOVIMENTACAO_ESTOQUE",
                        $"Não é possível remover os SKUs {string.Join(", ", skusComMovimentacoes)} porque possuem movimentações de estoque vinculadas. Cancele ou estorne essas movimentações antes de alterar as variações do produto."));
                }

                existente.Atualizar(command.Produto, command.Descricao, categoria, marca, unidadeMedida);
                if (command.Ativo) existente.Ativar(); else existente.Desativar();

                var atualizado = await _produtosRepository.AtualizarProduto(id, existente);

                foreach (var skuAtual in skusAtuais.Itens.Where(sku => skusParaExcluir.Contains(sku.Sku)))
                {
                    await _skusRepository.DeletarSku(skuAtual.Sku);
                }

                var skuIndex = skusAtuais.Itens.Count() + 1;
                foreach (var skuCommand in command.Skus)
                {
                    var isNew = string.IsNullOrWhiteSpace(skuCommand.Sku);
                    var skuCode = isNew ? $"{id}{skuIndex++}" : skuCommand.Sku!;

                    var sku = new Skus(skuCode, skuCommand.Preco, 0, skuCommand.Ativo, skuCommand.GtinEan);
                    if (skuCommand.AtributoValorIds != null && skuCommand.AtributoValorIds.Any())
                    {
                        var valoresAtributo = await _atributosRepository.ObterValoresPorIds(skuCommand.AtributoValorIds);
                        foreach (var valor in valoresAtributo) sku.AdicionarAtributo(valor);
                    }

                    if (isNew || !skusAtuais.Itens.Any(s => s.Sku == skuCode))
                    {
                        await _skusRepository.CriarSku(id, sku);
                    }
                    else
                    {
                        var skuExistente = skusAtuais.Itens.First(s => s.Sku == skuCode);
                        var skuParaUpdate = new Skus(skuCode, skuCommand.Preco, skuExistente.Estoque, skuCommand.Ativo, skuCommand.GtinEan, skuExistente.CustoMedio, skuExistente.CustoUltimaCompra);
                        skuParaUpdate.DefinirAtributos(sku.Atributos);
                        await _skusRepository.AtualizarSku(skuCode, skuParaUpdate);
                    }
                }

                _unitOfWork.Commit();
                return Resultado<Produtos>.Sucesso(atualizado);
            }
            catch
            {
                _unitOfWork.Rollback();
                throw;
            }
        });
    }

    public Task<bool> DeletarProduto(int id)
        => _produtosRepository.DeletarProduto(id);
}
