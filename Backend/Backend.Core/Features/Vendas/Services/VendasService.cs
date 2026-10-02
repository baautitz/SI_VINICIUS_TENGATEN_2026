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
using Backend.Core.Features.Estoque.Services;
using Backend.Core.Features.Estoque.Entities;
using Backend.Core.Features.Estoque.Entities.Enums;
using Backend.Core.Features.Estoque.Repositories;
using Backend.Core.Features.Parceiros.Entities;
using Backend.Core.Features.Financeiro.DTOs;
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
    private readonly MovimentacoesEstoquesService _estoqueService;
    private readonly IContasReceberRepository _contasRepository;
    private readonly IContasPagarRepository _contasPagarRepository;
    private readonly IDevolucoesRepository _devolucoesRepository;
    private readonly IMovimentacoesEstoquesRepository _movimentacoesRepository;
    private readonly IUnitOfWork _unitOfWork;

    public VendasService(
        IVendasRepository vendasRepository,
        IClientesRepository clientesRepository,
        IEmitentesRepository emitentesRepository,
        ISkusRepository skusRepository,
        ICondicoesPagamentosRepository condicoesRepository,
        MovimentacoesEstoquesService estoqueService,
        IContasReceberRepository contasRepository,
        IContasPagarRepository contasPagarRepository,
        IDevolucoesRepository devolucoesRepository,
        IMovimentacoesEstoquesRepository movimentacoesRepository,
        IUnitOfWork unitOfWork)
    {
        _vendasRepository = vendasRepository;
        _clientesRepository = clientesRepository;
        _emitentesRepository = emitentesRepository;
        _skusRepository = skusRepository;
        _condicoesRepository = condicoesRepository;
        _estoqueService = estoqueService;
        _contasRepository = contasRepository;
        _contasPagarRepository = contasPagarRepository;
        _devolucoesRepository = devolucoesRepository;
        _movimentacoesRepository = movimentacoesRepository;
        _unitOfWork = unitOfWork;
    }

    public Task<ResultadoPaginado<Venda>> ObterVendas(int pagina = 1, int tamanhoDaPagina = 20, int? clienteId = null)
        => _vendasRepository.ObterVendas(pagina, tamanhoDaPagina, clienteId);

    public Task<Venda?> ObterVendaPorId(int id)
        => _vendasRepository.ObterVendaPorId(id);

    public Task<ResultadoPaginado<Venda>> PesquisarVendas(string termo, int pagina = 1, int tamanhoDaPagina = 20, int? clienteId = null)
    {
        if (string.IsNullOrWhiteSpace(termo))
            return _vendasRepository.ObterVendas(pagina, tamanhoDaPagina, clienteId);

        return _vendasRepository.PesquisarVendas(termo, pagina, tamanhoDaPagina, clienteId);
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

                // Baixa de estoque: saída com origem VENDA
                var movimentacao = new MovimentacoesEstoques(
                    TipoMovimentacaoEstoque.SAIDA,
                    OrigemMovimentacaoEstoque.VENDA,
                    criada.Id,
                    observacao: $"Venda nº {criada.Id}");

                foreach (var itemCommand in command.Itens)
                {
                    var sku = await _skusRepository.ObterSkuPorSku(itemCommand.Sku);
                    movimentacao.AdicionarItem(sku!, itemCommand.Quantidade, sku!.CustoMedio);
                }

                var baixa = await _estoqueService.Registrar(movimentacao);
                if (!baixa.Success)
                    throw new DomainException(baixa.Errors!.First().Message);

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
                        origemTipo: OrigemTituloFinanceiro.VENDA,
                        origemId: criada.Id,
                        observacao: command.Observacao
                    );

                    foreach (var p in command.Parcelas)
                    {
                        conta.AdicionarParcela(p.NumeroParcela, p.DataVencimento, p.ValorParcela);
                        // Condição à vista (entrada mínima de 100%): a parcela nasce recebida.
                        if (EhAVista(condicao))
                            conta.RegistrarRecebimento(p.NumeroParcela, p.ValorParcela);
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

    private static bool EhAVista(CondicoesPagamentos? condicao) => condicao?.EntradaMinimaPercentual >= 100;

    private async Task<ContasReceber?> ObterContaDaVenda(int vendaId)
    {
        var contas = await _contasRepository.PesquisarContasReceber("", 1, 1, new FiltroContasReceber(OrigemTipo: OrigemTituloFinanceiro.VENDA, OrigemId: vendaId));
        var primeira = contas.Itens.FirstOrDefault();
        return primeira is null ? null : await _contasRepository.ObterContaReceberPorId(primeira.Id);
    }

    public Task<Devolucao?> ObterDevolucaoPorId(int id)
        => _devolucoesRepository.ObterDevolucaoPorId(id);

    public Task<IReadOnlyList<Devolucao>> ObterDevolucoesPorVenda(int vendaId)
        => _devolucoesRepository.ObterDevolucoesPorVenda(vendaId);

    // Devolução por item: estoque volta ao custo original da saída; o valor abate o saldo aberto da conta a receber
    // e o que excede (já recebido) vira conta a pagar ao cliente. Tudo na mesma transação.
    public async Task<Resultado<Devolucao>> CriarDevolucao(int vendaId, CriarDevolucaoCommand command)
    {
        var validation = await new CriarDevolucaoCommandValidator().ValidateAsync(command);
        if (!validation.IsValid)
            return Resultado<Devolucao>.Falha(validation.ToResultadoErros());

        var venda = await _vendasRepository.ObterVendaPorId(vendaId);
        if (venda is null)
            return Resultado<Devolucao>.Falha(new ResultadoErro("VENDA_INEXISTENTE", "A venda informada não existe.", "VendaId"));

        // Custo original = custo da movimentação de saída da venda (sku único por movimentação).
        var custos = (await _movimentacoesRepository.ObterMovimentacoesPorOrigem(OrigemMovimentacaoEstoque.VENDA, vendaId))
            .Where(m => m.TipoMovimentacao == TipoMovimentacaoEstoque.SAIDA)
            .SelectMany(m => m.MovimentacoesEstoquesItens)
            .ToDictionary(i => i.Sku.Sku, i => i.CustoUnitario);

        var itens = new List<DevolucaoItens>();
        foreach (var g in command.Itens.Where(i => i.Quantidade > 0).GroupBy(i => i.VendaItemId))
        {
            var itemVenda = venda.Itens.FirstOrDefault(i => i.Id == g.Key)
                ?? throw new DomainException($"O item {g.Key} não pertence à venda {vendaId}.");
            itens.Add(new DevolucaoItens(itemVenda, g.Sum(i => i.Quantidade), custos.GetValueOrDefault(itemVenda.Sku.Sku)));
        }

        var devolucao = new Devolucao(venda, command.Motivo, itens);

        return await ExecuteResultAsync(async () =>
        {
            try
            {
                _unitOfWork.BeginTransaction();

                var criada = await _devolucoesRepository.CriarDevolucao(devolucao);

                var entrada = new MovimentacoesEstoques(
                    TipoMovimentacaoEstoque.ENTRADA,
                    OrigemMovimentacaoEstoque.DEVOLUCAO_VENDA,
                    criada.Id,
                    observacao: $"Devolução nº {criada.Id} da venda nº {vendaId}");

                foreach (var item in criada.Itens)
                    entrada.AdicionarItem(venda.Itens.First(i => i.Id == item.VendaItemId).Sku, item.Quantidade, item.CustoUnitario);

                var resultadoEstoque = await _estoqueService.Registrar(entrada);
                if (!resultadoEstoque.Success)
                    throw new DomainException(resultadoEstoque.Errors!.First().Message);

                // Sem conta a receber (venda sem condição de pagamento) não há valor financeiro a reverter.
                var conta = await ObterContaDaVenda(vendaId);
                if (conta != null)
                {
                    var excedente = conta.AbaterSaldoAberto(criada.ValorTotal);
                    await _contasRepository.AtualizarContaReceber(conta.Id, conta);

                    if (excedente > 0)
                        await CriarReembolso(venda, criada, excedente);
                }

                _unitOfWork.Commit();
                return Resultado<Devolucao>.Sucesso(criada);
            }
            catch (Exception)
            {
                _unitOfWork.Rollback();
                throw;
            }
        });
    }

    private async Task CriarReembolso(Venda venda, Devolucao devolucao, decimal valor)
    {
        var cliente = await _clientesRepository.ObterClientePorId(venda.Cliente.Id)
            ?? throw new DomainException("Cliente da venda não encontrado.");

        var hoje = DateTime.Today;
        var conta = new ContasPagar(
            descricao: $"Reembolso da devolução nº {devolucao.Id} (venda nº {venda.Id})",
            valorOriginal: valor,
            fornecedor: null,
            clienteId: cliente.Id,
            clienteNome: cliente.NomeRazaoSocial,
            dataEmissao: hoje,
            origemTipo: OrigemTituloFinanceiro.DEVOLUCAO_VENDA,
            origemId: devolucao.Id);
        conta.AdicionarParcela(1, hoje, valor);

        await _contasPagarRepository.CriarContaPagar(conta);
    }
}
