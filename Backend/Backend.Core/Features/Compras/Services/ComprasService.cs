using Backend.Core.Common;
using Backend.Core.Common.Exceptions;
using Backend.Core.Common.Extensions;
using Backend.Core.Common.Interfaces;
using Backend.Core.Common.Results;
using Backend.Core.Features.Catalogo.Repositories;
using Backend.Core.Features.Compras.Commands;
using Backend.Core.Features.Compras.Entities;
using Backend.Core.Features.Compras.Repositories;
using Backend.Core.Features.Compras.Validators.Commands;
using Backend.Core.Features.Estoque.Entities;
using Backend.Core.Features.Estoque.Entities.Enums;
using Backend.Core.Features.Estoque.Services;
using Backend.Core.Features.Financeiro.Entities;
using Backend.Core.Features.Financeiro.Entities.Enums;
using Backend.Core.Features.Financeiro.Repositories;
using Backend.Core.Features.Parceiros.Repositories;

namespace Backend.Core.Features.Compras.Services;

public sealed class ComprasService : BaseService
{
    private readonly IComprasRepository _comprasRepository;
    private readonly IFornecedoresRepository _fornecedoresRepository;
    private readonly IEmitentesRepository _emitentesRepository;
    private readonly ITransportadorasRepository _transportadorasRepository;
    private readonly ISkusRepository _skusRepository;
    private readonly ICondicoesPagamentosRepository _condicoesRepository;
    private readonly MovimentacoesEstoquesService _estoqueService;
    private readonly IContasPagarRepository _contasPagarRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ComprasService(
        IComprasRepository comprasRepository,
        IFornecedoresRepository fornecedoresRepository,
        IEmitentesRepository emitentesRepository,
        ITransportadorasRepository transportadorasRepository,
        ISkusRepository skusRepository,
        ICondicoesPagamentosRepository condicoesRepository,
        MovimentacoesEstoquesService estoqueService,
        IContasPagarRepository contasPagarRepository,
        IUnitOfWork unitOfWork)
    {
        _comprasRepository = comprasRepository;
        _fornecedoresRepository = fornecedoresRepository;
        _emitentesRepository = emitentesRepository;
        _transportadorasRepository = transportadorasRepository;
        _skusRepository = skusRepository;
        _condicoesRepository = condicoesRepository;
        _estoqueService = estoqueService;
        _contasPagarRepository = contasPagarRepository;
        _unitOfWork = unitOfWork;
    }

    public Task<ResultadoPaginado<Compra>> PesquisarCompras(string? termo, int pagina = 1, int tamanhoDaPagina = 20, int? fornecedorId = null)
        => _comprasRepository.PesquisarCompras(termo, pagina, tamanhoDaPagina, fornecedorId);

    public Task<Compra?> ObterCompraPorId(int id) => _comprasRepository.ObterCompraPorId(id);

    public Task<IReadOnlyDictionary<string, string>> ObterVinculosDoFornecedor(int fornecedorId)
        => _comprasRepository.ObterVinculosDoFornecedor(fornecedorId);

    public async Task<Resultado<string>> VincularSku(int fornecedorId, string codigoProduto, string sku)
    {
        if (string.IsNullOrWhiteSpace(codigoProduto))
            return Resultado<string>.Falha(new ResultadoErro("CODIGO_OBRIGATORIO", "Código do produto no fornecedor é obrigatório.", "CodigoProduto"));
        if (await _fornecedoresRepository.ObterFornecedorPorId(fornecedorId) is null)
            return Resultado<string>.Falha(new ResultadoErro("FORNECEDOR_INEXISTENTE", "O fornecedor informado não existe.", "FornecedorId"));
        if (await _skusRepository.ObterSkuPorSku(sku) is null)
            return Resultado<string>.Falha(new ResultadoErro("SKU_INEXISTENTE", $"O SKU '{sku}' não existe.", "Sku"));

        await _comprasRepository.VincularSku(fornecedorId, codigoProduto.Trim(), sku);
        return Resultado<string>.Sucesso(sku);
    }

    // Compra = recebimento imediato: compra, entrada de estoque (custo rateado) e contas a pagar na mesma transação.
    public async Task<Resultado<Compra>> CriarCompra(CriarCompraCommand command)
    {
        var validation = await new CriarCompraCommandValidator().ValidateAsync(command);
        if (!validation.IsValid)
            return Resultado<Compra>.Falha(validation.ToResultadoErros());

        var fornecedor = await _fornecedoresRepository.ObterFornecedorPorId(command.FornecedorId);
        if (fornecedor is null)
            return Resultado<Compra>.Falha(new ResultadoErro("FORNECEDOR_INEXISTENTE", "O fornecedor informado não existe.", "FornecedorId"));

        var emitente = await _emitentesRepository.ObterEmitentePorId(command.EmitenteId);
        if (emitente is null)
            return Resultado<Compra>.Falha(new ResultadoErro("EMITENTE_INEXISTENTE", "O emitente informado não existe.", "EmitenteId"));

        Backend.Core.Features.Parceiros.Entities.Transportadoras? transportadora = null;
        if (command.TransportadoraId.HasValue)
        {
            transportadora = await _transportadorasRepository.ObterTransportadoraPorId(command.TransportadoraId.Value);
            if (transportadora is null)
                return Resultado<Compra>.Falha(new ResultadoErro("TRANSPORTADORA_INEXISTENTE", "A transportadora informada não existe.", "TransportadoraId"));
        }

        Backend.Core.Features.Financeiro.Entities.CondicoesPagamentos? condicao = null;
        if (command.CondicaoPagamentoId.HasValue)
        {
            condicao = await _condicoesRepository.ObterCondicaoPagamentoPorId(command.CondicaoPagamentoId.Value);
            if (condicao is null)
                return Resultado<Compra>.Falha(new ResultadoErro("CONDICAO_PAGAMENTO_INEXISTENTE", "A condição de pagamento informada não existe.", "CondicaoPagamentoId"));
        }

        var itens = new List<CompraItens>();
        foreach (var i in command.Itens)
        {
            var sku = await _skusRepository.ObterSkuPorSku(i.Sku);
            if (sku is null)
                return Resultado<Compra>.Falha(new ResultadoErro("SKU_INEXISTENTE", $"O SKU '{i.Sku}' não existe.", "Itens"));
            itens.Add(new CompraItens(i.Quantidade, i.ValorUnitario, i.ValorDesconto, sku, i.CodigoProdutoFornecedor));
        }

        var compra = new Compra(command.DataCompra, emitente, fornecedor, itens,
            command.ValorFrete, command.ValorSeguro, command.ValorOutrasDespesas, command.ValorDesconto,
            command.ValorFreteTerceiro, command.TransportadoraId, command.CondicaoPagamentoId, command.Observacao);

        return await ExecuteResultAsync(async () =>
        {
            try
            {
                _unitOfWork.BeginTransaction();

                var criada = await _comprasRepository.CriarCompra(compra);

                // Entrada de estoque ao custo rateado (todas as despesas); itens do mesmo SKU viram uma linha só.
                var entrada = new MovimentacoesEstoques(
                    TipoMovimentacaoEstoque.ENTRADA, OrigemMovimentacaoEstoque.COMPRA, criada.Id,
                    observacao: $"Compra nº {criada.Id}");

                foreach (var g in criada.Itens.GroupBy(i => i.Sku.Sku))
                {
                    var quantidade = g.Sum(i => i.Quantidade);
                    var custo = Math.Round(g.Sum(i => i.ValorTotal + i.ValorRateio) / quantidade, 4, MidpointRounding.AwayFromZero);
                    entrada.AdicionarItem(g.First().Sku, quantidade, custo);
                }

                var recebimento = await _estoqueService.Registrar(entrada);
                if (!recebimento.Success)
                    throw new DomainException(recebimento.Errors!.First().Message);

                foreach (var item in criada.Itens.Where(i => i.CodigoProdutoFornecedor != null))
                    await _comprasRepository.VincularSku(fornecedor.Id, item.CodigoProdutoFornecedor!, item.Sku.Sku);

                var aVista = condicao?.EntradaMinimaPercentual >= 100;
                var conta = new ContasPagar(
                    descricao: $"Compra nº {criada.Id}", valorOriginal: criada.ValorNota, fornecedor: fornecedor,
                    dataEmissao: criada.DataCompra, condicaoPagamento: condicao,
                    origemTipo: OrigemTituloFinanceiro.COMPRA, origemId: criada.Id, observacao: criada.Observacao);

                foreach (var p in command.Parcelas!)
                {
                    conta.AdicionarParcela(p.NumeroParcela, p.DataVencimento, p.ValorParcela);
                    if (aVista)
                        conta.RegistrarPagamento(p.NumeroParcela, p.ValorParcela);
                }
                await _contasPagarRepository.CriarContaPagar(conta);

                // Frete de terceiro (FOB/CT-e): conta separada à transportadora, parcela única.
                if (criada.ValorFreteTerceiro > 0)
                {
                    var frete = new ContasPagar(
                        descricao: $"Frete da compra nº {criada.Id}", valorOriginal: criada.ValorFreteTerceiro, fornecedor: null,
                        dataEmissao: criada.DataCompra, origemTipo: OrigemTituloFinanceiro.COMPRA, origemId: criada.Id,
                        transportadoraId: transportadora!.Id);
                    frete.AdicionarParcela(1, command.VencimentoFreteTerceiro!.Value, criada.ValorFreteTerceiro);
                    await _contasPagarRepository.CriarContaPagar(frete);
                }

                _unitOfWork.Commit();
                return Resultado<Compra>.Sucesso(criada);
            }
            catch (Exception)
            {
                _unitOfWork.Rollback();
                throw;
            }
        });
    }
}
