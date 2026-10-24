using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend.Core.Common;
using Backend.Core.Common.Extensions;
using Backend.Core.Common.Results;
using Backend.Core.Common.Interfaces;
using Backend.Core.Common.Exceptions;
using Backend.Core.Features.Vendas.DTOs;
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
using Backend.Core.Features.NFe.Repositories;
using Backend.Core.Features.Vendas.Repositories;
using Backend.Core.Features.Vendas.Entities;
using Backend.Core.Features.Vendas.Entities.Enums;
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
    private readonly INfesRepository _nfesRepository;
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
        INfesRepository nfesRepository,
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
        _nfesRepository = nfesRepository;
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
                    var parcelas = command.Parcelas.ToList();
                    foreach (var p in parcelas)
                    {
                        var conta = new ContasReceber(
                            descricao: $"Venda nº {criada.Id}",
                            valor: p.ValorParcela,
                            cliente: cliente,
                            dataVencimento: p.DataVencimento,
                            numeroDocumento: $"VENDA-{criada.Id}",
                            numeroParcela: p.NumeroParcela,
                            totalParcelas: parcelas.Count,
                            dataEmissao: command.DataVenda,
                            condicaoPagamento: condicao,
                            origemTipo: OrigemTituloFinanceiro.VENDA,
                            origemId: criada.Id,
                            observacao: command.Observacao
                        );
                        // Condição à vista (entrada mínima de 100%): a parcela nasce recebida.
                        if (EhAVista(condicao))
                            conta.RegistrarRecebimento(p.ValorParcela);
                        await _contasRepository.CriarContaReceber(conta);
                    }
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

    // Todas as parcelas (linhas) da venda, por parcela; pagina até o fim, sem teto fixo.
    public async Task<IReadOnlyList<ContasReceber>> ObterContasDaVenda(int vendaId)
    {
        var todas = new List<ContasReceber>();
        for (var pagina = 1; ; pagina++)
        {
            var r = await _contasRepository.PesquisarContasReceber("", pagina, 100, new FiltroContasReceber(OrigemTipo: OrigemTituloFinanceiro.VENDA, OrigemId: vendaId));
            var antes = todas.Count;
            todas.AddRange(r.Itens);
            if (todas.Count == antes || todas.Count >= r.TotalDeItens) break;
        }

        return todas.OrderBy(c => c.NumeroParcela).ToList();
    }

    public Task<Devolucao?> ObterDevolucaoPorId(int id)
        => _devolucoesRepository.ObterDevolucaoPorId(id);

    public Task<ResultadoPaginado<DevolucaoResumo>> PesquisarDevolucoes(string termo, int pagina = 1, int tamanhoDaPagina = 20, int? vendaId = null)
        => _devolucoesRepository.PesquisarDevolucoes(termo, pagina, tamanhoDaPagina, vendaId);

    public Task<IReadOnlyList<Devolucao>> ObterDevolucoesPorVenda(int vendaId)
        => _devolucoesRepository.ObterDevolucoesPorVenda(vendaId);

    // Devolução por item: estoque volta ao custo original da saída; o valor abate o saldo em aberto das parcelas a receber
    // (registrando quanto saiu de cada uma) e o excedente (já recebido) vira conta a pagar ao cliente. Tudo na mesma transação.
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

                // Abate o saldo em aberto da última parcela para a primeira (preserva os vencimentos mais próximos);
                // o Valor original das parcelas não muda. O que não couber no aberto (já recebido) vira reembolso.
                var restante = criada.ValorTotal;
                foreach (var conta in (await ObterContasDaVenda(vendaId)).OrderByDescending(c => c.NumeroParcela))
                {
                    if (restante <= 0) break;
                    var abatido = conta.Abater(restante);
                    if (abatido <= 0) continue;
                    await _contasRepository.AtualizarContaReceber(conta.Id, conta);
                    await _devolucoesRepository.CriarAbatimento(new DevolucaoAbatimento(criada.Id, conta.Id, abatido));
                    restante -= abatido;
                }

                if (restante > 0)
                    await CriarReembolso(venda, criada, restante);

                _unitOfWork.Commit();
                // Só avisa: o cancelamento da NF-e é decisão fiscal separada.
                criada.NfeAtiva = await _nfesRepository.ObterNfeEmitidaDaVenda(vendaId);
                return Resultado<Devolucao>.Sucesso(criada);
            }
            catch (Exception)
            {
                _unitOfWork.Rollback();
                throw;
            }
        });
    }

    public const string DevolucaoInexistente = "DEVOLUCAO_INEXISTENTE";
    public const string DevolucaoJaEstornada = "DEVOLUCAO_JA_ESTORNADA";

    // Desfaz a devolução na mesma transação: itens saem de novo do estoque, os abatimentos voltam às parcelas a receber
    // e o reembolso ao cliente (conta a pagar) é cancelado, barrado se já tiver pagamento. Não mexe na NF-e: só avisa em NfeAtiva.
    public async Task<Resultado<Devolucao>> EstornarDevolucao(int id, EstornarDevolucaoCommand command)
    {
        var motivo = command?.Motivo?.Trim() ?? "";
        if (motivo.Length < 5 || motivo.Length > 255)
            return Resultado<Devolucao>.Falha(new ResultadoErro("MOTIVO_INVALIDO", "O motivo do estorno deve ter entre 5 e 255 caracteres.", "motivo"));

        var devolucao = await _devolucoesRepository.ObterDevolucaoPorId(id);
        if (devolucao is null)
            return Resultado<Devolucao>.Falha(new ResultadoErro(DevolucaoInexistente, "Devolução não encontrada."));
        if (devolucao.Status == StatusDevolucao.ESTORNADA)
            return Resultado<Devolucao>.Falha(new ResultadoErro(DevolucaoJaEstornada, "Esta devolução já foi estornada.", "Status"));

        return await ExecuteResultAsync(async () =>
        {
            try
            {
                _unitOfWork.BeginTransaction();
                var resultado = await EstornarDevolucaoNaTransacao(devolucao, motivo);
                if (resultado.Success) _unitOfWork.Commit(); else _unitOfWork.Rollback();
                return resultado;
            }
            catch (Exception)
            {
                _unitOfWork.Rollback();
                throw;
            }
        });
    }

    private async Task<Resultado<Devolucao>> EstornarDevolucaoNaTransacao(Devolucao devolucao, string motivo)
    {
        // UPDATE guardado primeiro: trava a linha, então um estorno concorrente espera e cai em JA_ESTORNADA.
        devolucao.Estornar(motivo);
        if (!await _devolucoesRepository.MarcarEstornada(devolucao))
            return Resultado<Devolucao>.Falha(new ResultadoErro(DevolucaoJaEstornada, "Esta devolução já foi estornada.", "Status"));

        await _skusRepository.TravarSkus(devolucao.Itens.Select(i => i.Sku));

        var reembolsos = (await _contasPagarRepository.PesquisarContasPagar("", 1, 100,
                new FiltroContasPagar(OrigemTipo: OrigemTituloFinanceiro.DEVOLUCAO_VENDA, OrigemId: devolucao.Id))).Itens
            .Where(c => c.OrigemTipo == OrigemTituloFinanceiro.DEVOLUCAO_VENDA && c.OrigemId == devolucao.Id && c.Status != StatusTituloFinanceiro.CANCELADO)
            .ToList();

        var erros = new List<ResultadoErro>();
        foreach (var conta in reembolsos.Where(c => c.PossuiBaixa))
            erros.Add(new ResultadoErro("ESTORNO_PARCELA_BAIXADA",
                $"O reembolso '{conta.Descricao}' já tem R$ {conta.ValorPago:0.00} pago. Estorne o pagamento primeiro.", "ContasPagar"));
        erros.AddRange(await _estoqueService.ObterBloqueiosDeEstornoDeEntrada(OrigemMovimentacaoEstoque.DEVOLUCAO_VENDA, devolucao.Id));
        if (erros.Count > 0)
            return Resultado<Devolucao>.Falha(erros);

        await _estoqueService.EstornarPorOrigem(OrigemMovimentacaoEstoque.DEVOLUCAO_VENDA, devolucao.Id, $"Estorno da devolução nº {devolucao.Id}: {motivo}");

        // Reverte exatamente o que a devolução abateu de cada parcela; os registros são marcados, não apagados.
        var contasVenda = (await ObterContasDaVenda(devolucao.VendaId)).ToDictionary(c => c.Id);
        foreach (var abatimento in await _devolucoesRepository.ObterAbatimentosAtivos(devolucao.Id))
        {
            if (!contasVenda.TryGetValue(abatimento.ContaReceberId, out var conta)) continue;
            conta.Restituir(abatimento.Valor);
            await _contasRepository.AtualizarContaReceber(conta.Id, conta);
        }
        await _devolucoesRepository.MarcarAbatimentosEstornados(devolucao.Id, devolucao.EstornadoEm ?? DateTime.UtcNow);

        foreach (var reembolso in reembolsos)
        {
            reembolso.Cancelar();
            await _contasPagarRepository.AtualizarContaPagar(reembolso.Id, reembolso);
        }

        // Só avisa: a NF-e (cancelamento ou nota complementar) é decisão fiscal separada.
        devolucao.NfeAtiva = await _nfesRepository.ObterNfeEmitidaDaVenda(devolucao.VendaId);
        return Resultado<Devolucao>.Sucesso(devolucao);
    }

    private async Task CriarReembolso(Venda venda, Devolucao devolucao, decimal valor)
    {
        var cliente = await _clientesRepository.ObterClientePorId(venda.Cliente.Id)
            ?? throw new DomainException("Cliente da venda não encontrado.");

        var data = devolucao.DataDevolucao.Date;
        var conta = new ContasPagar(
            descricao: $"Reembolso da devolução nº {devolucao.Id} (venda nº {venda.Id})",
            valor: valor,
            fornecedor: null,
            dataVencimento: data,
            numeroDocumento: $"DEV-{devolucao.Id}",
            clienteId: cliente.Id,
            clienteNome: cliente.NomeRazaoSocial,
            dataEmissao: data,
            origemTipo: OrigemTituloFinanceiro.DEVOLUCAO_VENDA,
            origemId: devolucao.Id);
        await _contasPagarRepository.CriarContaPagar(conta);
    }
}
