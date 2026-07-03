using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend.Core.Common;
using Backend.Core.Common.Extensions;
using Backend.Core.Common.Results;
using Backend.Core.Common.Interfaces;
using Backend.Core.Common.Exceptions;
using Backend.Core.Features.Parceiros.Repositories;
using Backend.Core.Features.Catalogo.Repositories;
using Backend.Core.Features.Estoque.Repositories;
using Backend.Core.Features.Estoque.Entities;
using Backend.Core.Features.Estoque.Entities.Enums;
using Backend.Core.Features.Financeiro.Repositories;
using Backend.Core.Features.Financeiro.Entities;
using Backend.Core.Features.Financeiro.Entities.Enums;
using Backend.Core.Features.Vendas.Repositories;
using Backend.Core.Features.Vendas.Entities;
using Backend.Core.Features.Vendas.Commands;
using Backend.Core.Features.Vendas.Validators.Commands;

namespace Backend.Core.Features.Vendas.Services;

public sealed class VendasService : BaseService
{
    private readonly IVendasRepository _vendasRepository;
    private readonly IClientesRepository _clientesRepository;
    private readonly IEmitentesRepository _emitentesRepository;
    private readonly ISkusRepository _skusRepository;
    private readonly ICondicoesPagamentosRepository _condicoesRepository;
    private readonly IMovimentacoesEstoquesRepository _movimentacoesRepository;
    private readonly IContasReceberRepository _contasRepository;
    private readonly IUnitOfWork _unitOfWork;

    public VendasService(
        IVendasRepository vendasRepository,
        IClientesRepository clientesRepository,
        IEmitentesRepository emitentesRepository,
        ISkusRepository skusRepository,
        ICondicoesPagamentosRepository condicoesRepository,
        IMovimentacoesEstoquesRepository movimentacoesRepository,
        IContasReceberRepository contasRepository,
        IUnitOfWork unitOfWork)
    {
        _vendasRepository = vendasRepository;
        _clientesRepository = clientesRepository;
        _emitentesRepository = emitentesRepository;
        _skusRepository = skusRepository;
        _condicoesRepository = condicoesRepository;
        _movimentacoesRepository = movimentacoesRepository;
        _contasRepository = contasRepository;
        _unitOfWork = unitOfWork;
    }

    public Task<ResultadoPaginado<Venda>> ObterVendas(int pagina = 1, int tamanhoDaPagina = 20)
        => _vendasRepository.ObterVendas(pagina, tamanhoDaPagina);

    public Task<Venda?> ObterVendaPorId(int id)
        => _vendasRepository.ObterVendaPorId(id);

    public Task<ResultadoPaginado<DTOs.VendasResumo>> PesquisarVendas(string termo, int pagina = 1, int tamanhoDaPagina = 20)
    {
        if (string.IsNullOrWhiteSpace(termo))
            return _vendasRepository.ObterVendasResumo(pagina, tamanhoDaPagina);

        return _vendasRepository.PesquisarVendas(termo, pagina, tamanhoDaPagina);
    }

    public async Task<Resultado<Venda>> CriarVenda(CriarVendaCommand command)
    {
        var validator = new CriarVendaCommandValidator();
        var validation = await validator.ValidateAsync(command);
        if (!validation.IsValid)
            return Resultado<Venda>.Falha(validation.ToResultadoErros());

        var cliente = await _clientesRepository.ObterClientePorId(command.ClienteId);
        if (cliente is null)
            return Resultado<Venda>.Falha(new ResultadoErro("CLIENTE_INEXISTENTE", "O cliente informado não existe.", "ClienteId"));

        var emitente = await _emitentesRepository.ObterEmitentePorId(command.EmitenteId);
        if (emitente is null)
            return Resultado<Venda>.Falha(new ResultadoErro("EMITENTE_INEXISTENTE", "O emitente informado não existe.", "EmitenteId"));

        CondicoesPagamentos? condicao = null;
        if (command.CondicaoPagamentoId.HasValue)
        {
            condicao = await _condicoesRepository.ObterCondicaoPagamentoPorId(command.CondicaoPagamentoId.Value);
            if (condicao is null)
                return Resultado<Venda>.Falha(new ResultadoErro("CONDICAO_PAGAMENTO_INEXISTENTE", "A condição de pagamento informada não existe.", "CondicaoPagamentoId"));
        }

        // Verify stock levels before starting transaction
        foreach (var itemCommand in command.Itens)
        {
            var sku = await _skusRepository.ObterSkuPorSku(itemCommand.Sku);
            if (sku == null)
                return Resultado<Venda>.Falha(new ResultadoErro("SKU_INEXISTENTE", $"O SKU '{itemCommand.Sku}' não existe.", "Itens"));

            if (sku.Estoque < itemCommand.Quantidade)
                return Resultado<Venda>.Falha(new ResultadoErro("ESTOQUE_INSUFICIENTE", $"Estoque insuficiente para o SKU '{sku.Sku}'. Disponível: {sku.Estoque:0.####}, Solicitado: {itemCommand.Quantidade:0.####}", "Itens"));
        }

        return await ExecuteResultAsync(async () =>
        {
            try
            {
                _unitOfWork.BeginTransaction();

                var vendaItensList = new List<VendaItens>();
                foreach (var itemCommand in command.Itens)
                {
                    var sku = await _skusRepository.ObterSkuPorSku(itemCommand.Sku);
                    // Sku exists check was done above
                    var item = new VendaItens(itemCommand.Quantidade, itemCommand.ValorUnitario, itemCommand.ValorDesconto, sku!);
                    vendaItensList.Add(item);
                }

                var venda = new Venda(
                    command.DataVenda,
                    emitente,
                    cliente,
                    vendaItensList,
                    command.Observacao
                );

                var criada = await _vendasRepository.CriarVenda(venda);

                // Create Stock Movement (VENDA)
                var movimentacao = new MovimentacoesEstoques(
                    TipoMovimentacaoEstoque.VENDA,
                    usuario: null,
                    nfeId: null,
                    vendaId: criada.Id,
                    observacao: $"Venda nº {criada.Id}",
                    status: StatusMovimentacaoEstoque.RASCUNHO
                );

                foreach (var itemCommand in command.Itens)
                {
                    var sku = await _skusRepository.ObterSkuPorSku(itemCommand.Sku);
                    if (sku != null)
                    {
                        var produtoNome = sku.NomeExibicao;
                        var unidadeMedidaSigla = sku.Produto!.UnidadeMedida.Sigla;

                        // Add item to stock movement
                        movimentacao.AdicionarItem(sku, itemCommand.Quantidade, sku.CustoMedio, produtoNome, unidadeMedidaSigla);

                        // Update physical SKU stock
                        var itemMov = movimentacao.MovimentacoesEstoquesItens.Last();
                        itemMov.DefinirQuantidadesECustosAnteriores(sku.Estoque, sku.CustoMedio);
                        sku.AjustarEstoque(-itemCommand.Quantidade);
                        await _skusRepository.AtualizarSku(sku.Sku, sku);
                    }
                }

                movimentacao.Confirmar();
                await _movimentacoesRepository.CriarMovimentacao(movimentacao);

                // Create Accounts Receivable (ContasReceber)
                if (condicao != null && command.Parcelas != null && command.Parcelas.Any())
                {
                    var conta = new ContasReceber(
                        descricao: $"Venda nº {criada.Id}",
                        valorOriginal: criada.ValorTotal,
                        cliente: cliente,
                        dataEmissao: command.DataVenda,
                        dataVencimento: null,
                        condicaoPagamento: condicao,
                        nfeId: null,
                        vendaId: criada.Id,
                        observacao: command.Observacao
                    );

                    foreach (var p in command.Parcelas)
                    {
                        conta.AdicionarParcela(p.NumeroParcela, p.DataVencimento, p.ValorParcela);
                    }

                    await _contasRepository.CriarContaReceber(conta);
                }

                _unitOfWork.Commit();
                return Resultado<Venda>.Sucesso(criada);
            }
            catch (Exception)
            {
                _unitOfWork.Rollback();
                throw;
            }
        });
    }

    public async Task<bool> DeletarVenda(int id)
    {
        var venda = await _vendasRepository.ObterVendaPorId(id);
        if (venda is null)
            return false;

        try
        {
            _unitOfWork.BeginTransaction();

            // Reverse Stock Movements related to this Venda
            var movimentacoes = await _movimentacoesRepository.ObterMovimentacoes(1, 100);
            var movVenda = movimentacoes.Itens.FirstOrDefault(m => m.VendaId == id && m.Status == StatusMovimentacaoEstoque.CONFIRMADA);
            if (movVenda != null)
            {
                foreach (var item in movVenda.MovimentacoesEstoquesItens)
                {
                    var sku = await _skusRepository.ObterSkuPorSku(item.Sku.Sku);
                    if (sku != null)
                    {
                        // Return the items to physical stock
                        sku.AjustarEstoque(item.Quantidade);
                        await _skusRepository.AtualizarSku(sku.Sku, sku);
                    }
                }
                movVenda.Cancelar();
                await _movimentacoesRepository.AtualizarMovimentacao(movVenda.Id, movVenda);
            }

            // Cancel Accounts Receivable related to this Venda
            var contas = await _contasRepository.ObterContasReceber(1, 100);
            var contaVenda = contas.Itens.FirstOrDefault(c => c.VendaId == id);
            if (contaVenda != null)
            {
                // Check if any installments were already paid
                var temPagas = contaVenda.ContasReceberParcelas.Any(p => p.Status == StatusTituloFinanceiro.PAGO || p.Status == StatusTituloFinanceiro.PARCIAL || p.ValorRecebido > 0);
                if (temPagas)
                    throw new DomainException("Não é possível deletar uma venda com parcelas financeiras já pagas ou parciais.");

                // Delete or cancel the title
                await _contasRepository.DeletarContaReceber(contaVenda.Id);
            }

            var deleted = await _vendasRepository.DeletarVenda(id);
            _unitOfWork.Commit();
            return deleted;
        }
        catch (Exception)
        {
            _unitOfWork.Rollback();
            throw;
        }
    }
}
