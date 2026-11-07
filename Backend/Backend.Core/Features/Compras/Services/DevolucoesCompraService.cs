using Backend.Core.Common;
using Backend.Core.Common.Interfaces;
using Backend.Core.Common.Results;
using Backend.Core.Features.Catalogo.Repositories;
using Backend.Core.Features.Compras.Commands;
using Backend.Core.Features.Compras.Entities;
using Backend.Core.Features.Compras.Entities.Enums;
using Backend.Core.Features.Compras.Repositories;
using Backend.Core.Features.Estoque.Entities;
using Backend.Core.Features.Estoque.Entities.Enums;
using Backend.Core.Features.Estoque.Services;
using Backend.Core.Features.Financeiro.DTOs;
using Backend.Core.Features.Financeiro.Entities;
using Backend.Core.Features.Financeiro.Entities.Enums;
using Backend.Core.Features.Financeiro.Policies;
using Backend.Core.Features.Financeiro.Repositories;

namespace Backend.Core.Features.Compras.Services;

public sealed class DevolucoesCompraService : BaseService
{
    public const string CompraInexistente = "COMPRA_INEXISTENTE";
    public const string DevolucaoInexistente = "DEVOLUCAO_INEXISTENTE";
    public const string DevolucaoJaEstornada = "DEVOLUCAO_JA_ESTORNADA";

    private readonly IComprasRepository _comprasRepository;
    private readonly IDevolucoesCompraRepository _devolucoesRepository;
    private readonly ISkusRepository _skusRepository;
    private readonly MovimentacoesEstoquesService _estoqueService;
    private readonly IContasPagarRepository _contasPagarRepository;
    private readonly IContasReceberRepository _contasReceberRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DevolucoesCompraService(
        IComprasRepository comprasRepository,
        IDevolucoesCompraRepository devolucoesRepository,
        ISkusRepository skusRepository,
        MovimentacoesEstoquesService estoqueService,
        IContasPagarRepository contasPagarRepository,
        IContasReceberRepository contasReceberRepository,
        IUnitOfWork unitOfWork)
    {
        _comprasRepository = comprasRepository;
        _devolucoesRepository = devolucoesRepository;
        _skusRepository = skusRepository;
        _estoqueService = estoqueService;
        _contasPagarRepository = contasPagarRepository;
        _contasReceberRepository = contasReceberRepository;
        _unitOfWork = unitOfWork;
    }

    public Task<DevolucaoCompra?> ObterDevolucaoPorId(int id) => _devolucoesRepository.ObterDevolucaoPorId(id);

    public Task<IReadOnlyList<DevolucaoCompra>> ObterDevolucoesPorCompra(int compraId)
        => _devolucoesRepository.ObterDevolucoesPorCompra(compraId);

    // Saída de estoque pelo custo da compra, que também sai do custo médio; o valor abate o saldo em aberto das
    // parcelas a pagar (na ordem de OrdemAbatimento, registrando quanto saiu de cada uma) e o excedente (já pago)
    // vira conta a receber do fornecedor. Tudo numa transação: com qualquer
    // recusa nada é gravado. Os SKUs são travados antes de conferir saldo e estoque, então devoluções
    // e vendas concorrentes esperam.
    public async Task<Resultado<DevolucaoCompra>> CriarDevolucao(int compraId, CriarDevolucaoCompraCommand command)
    {
        var motivo = command?.Motivo?.Trim() ?? "";
        if (motivo.Length < 5 || motivo.Length > 255)
            return Resultado<DevolucaoCompra>.Falha(new ResultadoErro("MOTIVO_INVALIDO", "O motivo da devolução deve ter entre 5 e 255 caracteres.", "Motivo"));

        var pedidos = (command!.Itens ?? Array.Empty<CriarDevolucaoCompraItemCommand>())
            .Where(i => i.Quantidade != 0).GroupBy(i => i.CompraItemId)
            .Select(g => new CriarDevolucaoCompraItemCommand(g.Key, g.Sum(i => i.Quantidade))).ToList();
        if (pedidos.Count == 0)
            return Resultado<DevolucaoCompra>.Falha(new ResultadoErro("ITENS_OBRIGATORIOS", "Informe ao menos um item com quantidade maior que zero.", "Itens"));
        if (pedidos.Any(i => i.Quantidade < 0))
            return Resultado<DevolucaoCompra>.Falha(new ResultadoErro("QUANTIDADE_INVALIDA", "A quantidade devolvida não pode ser negativa.", "Itens"));

        var compra = await _comprasRepository.ObterCompraPorId(compraId);
        if (compra is null)
            return Resultado<DevolucaoCompra>.Falha(new ResultadoErro(CompraInexistente, "Compra não encontrada."));

        return await ExecuteResultAsync(async () =>
        {
            try
            {
                _unitOfWork.BeginTransaction();
                var resultado = await CriarNaTransacao(compra, motivo, pedidos);
                if (resultado.Success) _unitOfWork.Commit(); else _unitOfWork.Rollback();
                return resultado;
            }
            catch
            {
                _unitOfWork.Rollback();
                throw;
            }
        });
    }

    private async Task<Resultado<DevolucaoCompra>> CriarNaTransacao(Compra compra, string motivo, List<CriarDevolucaoCompraItemCommand> pedidos)
    {
        await _skusRepository.TravarSkus(compra.Itens.Select(i => i.Sku.Sku).Distinct().OrderBy(s => s));

        // Relê já sob a trava: o saldo devolvível reflete devoluções concorrentes.
        compra = await _comprasRepository.ObterCompraPorId(compra.Id) ?? compra;
        if (compra.Estornada)
            return Resultado<DevolucaoCompra>.Falha(new ResultadoErro("COMPRA_ESTORNADA", "Esta compra foi estornada e não aceita devolução.", "CompraId"));

        var itens = new List<DevolucaoCompraItens>();
        var erros = new List<ResultadoErro>();
        foreach (var pedido in pedidos)
        {
            var itemCompra = compra.Itens.FirstOrDefault(i => i.Id == pedido.CompraItemId);
            if (itemCompra is null)
            {
                erros.Add(new ResultadoErro("ITEM_INEXISTENTE", $"O item {pedido.CompraItemId} não pertence à compra {compra.Id}.", "Itens"));
                continue;
            }
            if (pedido.Quantidade > itemCompra.SaldoDevolvivel)
            {
                erros.Add(new ResultadoErro("DEVOLUCAO_ACIMA_DO_SALDO",
                    $"Quantidade devolvida do SKU '{itemCompra.Sku.Sku}' excede o saldo devolvível da compra ({itemCompra.SaldoDevolvivel:0.####}).", "Itens"));
                continue;
            }
            itens.Add(new DevolucaoCompraItens(itemCompra, pedido.Quantidade));
        }
        if (erros.Count > 0)
            return Resultado<DevolucaoCompra>.Falha(erros);

        // O que já foi vendido não volta ao fornecedor: o limite é o saldo atual do SKU.
        var skus = new Dictionary<string, Catalogo.Entities.Skus>();
        foreach (var grupo in itens.GroupBy(i => i.Sku))
        {
            var sku = await _skusRepository.ObterSkuPorSku(grupo.Key);
            if (sku is null)
            {
                erros.Add(new ResultadoErro("SKU_INEXISTENTE", $"O SKU '{grupo.Key}' não existe.", "Itens"));
                continue;
            }
            skus[sku.Sku] = sku;
            var quantidade = grupo.Sum(i => i.Quantidade);
            if (quantidade > sku.Estoque)
                erros.Add(new ResultadoErro("DEVOLUCAO_ACIMA_DO_ESTOQUE",
                    $"SKU '{sku.Sku}': só há {sku.Estoque:0.####} em estoque e a devolução pede {quantidade:0.####}; o que já foi vendido ou baixado não pode ser devolvido ao fornecedor.", "Itens"));
        }
        if (erros.Count > 0)
            return Resultado<DevolucaoCompra>.Falha(erros);

        var criada = await _devolucoesRepository.CriarDevolucao(new DevolucaoCompra(compra, motivo, itens));

        var saida = new MovimentacoesEstoques(
            TipoMovimentacaoEstoque.SAIDA, OrigemMovimentacaoEstoque.DEVOLUCAO_COMPRA, criada.Id,
            observacao: $"Devolução nº {criada.Id} da compra nº {compra.Id}");
        foreach (var item in criada.Itens)
            saida.AdicionarItem(skus[item.Sku], item.Quantidade, item.CustoUnitario);

        var estoque = await _estoqueService.Registrar(saida);
        if (!estoque.Success)
            return Resultado<DevolucaoCompra>.Falha(estoque.Errors!);

        await AbaterParcelasEReembolsar(compra, criada);

        return Resultado<DevolucaoCompra>.Sucesso(criada);
    }

    // Desfaz a devolução na mesma transação: os itens voltam ao estoque, os abatimentos voltam às parcelas a pagar
    // e o reembolso do fornecedor (conta a receber) é cancelado, barrado se já tiver recebimento.
    public async Task<Resultado<DevolucaoCompra>> EstornarDevolucao(int id, EstornarDevolucaoCompraCommand command)
    {
        var motivo = command?.Motivo?.Trim() ?? "";
        if (motivo.Length < 5 || motivo.Length > 255)
            return Resultado<DevolucaoCompra>.Falha(new ResultadoErro("MOTIVO_INVALIDO", "O motivo do estorno deve ter entre 5 e 255 caracteres.", "motivo"));

        var devolucao = await _devolucoesRepository.ObterDevolucaoPorId(id);
        if (devolucao is null)
            return Resultado<DevolucaoCompra>.Falha(new ResultadoErro(DevolucaoInexistente, "Devolução não encontrada."));
        if (devolucao.Status == StatusDevolucaoCompra.ESTORNADA)
            return Resultado<DevolucaoCompra>.Falha(new ResultadoErro(DevolucaoJaEstornada, "Esta devolução já foi estornada.", "Status"));

        return await ExecuteResultAsync(async () =>
        {
            try
            {
                _unitOfWork.BeginTransaction();
                var resultado = await EstornarNaTransacao(devolucao, motivo);
                if (resultado.Success) _unitOfWork.Commit(); else _unitOfWork.Rollback();
                return resultado;
            }
            catch
            {
                _unitOfWork.Rollback();
                throw;
            }
        });
    }

    private async Task<Resultado<DevolucaoCompra>> EstornarNaTransacao(DevolucaoCompra devolucao, string motivo)
    {
        // UPDATE guardado primeiro: trava a linha, então um estorno concorrente espera e cai em JA_ESTORNADA.
        devolucao.Estornar(motivo);
        if (!await _devolucoesRepository.MarcarEstornada(devolucao))
            return Resultado<DevolucaoCompra>.Falha(new ResultadoErro(DevolucaoJaEstornada, "Esta devolução já foi estornada.", "Status"));

        await _skusRepository.TravarSkus(devolucao.Itens.Select(i => i.Sku).Distinct().OrderBy(s => s));

        var reembolsos = (await _contasReceberRepository.PesquisarContasReceber("", 1, 100,
                new FiltroContasReceber(OrigemTipo: OrigemTituloFinanceiro.DEVOLUCAO_COMPRA, OrigemId: devolucao.Id))).Itens
            .Where(c => c.OrigemTipo == OrigemTituloFinanceiro.DEVOLUCAO_COMPRA && c.OrigemId == devolucao.Id && c.Status != StatusTituloFinanceiro.CANCELADO)
            .ToList();

        var baixados = reembolsos.Where(c => c.PossuiBaixa).ToList();
        if (baixados.Count > 0)
            return Resultado<DevolucaoCompra>.Falha(baixados.Select(c => new ResultadoErro("ESTORNO_REEMBOLSO_BAIXADO",
                $"O reembolso '{c.Descricao}' já tem R$ {c.ValorPago:0.00} recebido. Estorne o recebimento primeiro.", "ContasReceber")).ToList());

        await _estoqueService.EstornarPorOrigem(OrigemMovimentacaoEstoque.DEVOLUCAO_COMPRA, devolucao.Id, $"Estorno da devolução de compra nº {devolucao.Id}: {motivo}");

        // Reverte exatamente o que a devolução abateu de cada parcela; os registros são marcados, não apagados.
        var contasCompra = (await ObterContasDaCompra(devolucao.CompraId)).ToDictionary(c => c.Id);
        foreach (var abatimento in await _devolucoesRepository.ObterAbatimentosAtivos(devolucao.Id))
        {
            if (!contasCompra.TryGetValue(abatimento.ContaPagarId, out var conta)) continue;
            conta.Restituir(abatimento.Valor);
            await _contasPagarRepository.AtualizarContaPagar(conta.Id, conta);
        }
        await _devolucoesRepository.MarcarAbatimentosEstornados(devolucao.Id, devolucao.EstornadoEm ?? DateTime.UtcNow);

        foreach (var reembolso in reembolsos)
        {
            reembolso.Cancelar();
            await _contasReceberRepository.AtualizarContaReceber(reembolso.Id, reembolso);
        }

        return Resultado<DevolucaoCompra>.Sucesso(devolucao);
    }

    // O valor pago nunca é consumido: o que não couber no saldo em aberto vira Reembolso (título único, vence na data da devolução).
    private async Task AbaterParcelasEReembolsar(Compra compra, DevolucaoCompra devolucao)
    {
        var restante = devolucao.ValorTotal;
        var contas = OrdemAbatimento.Ordenar(await ObterContasDaCompra(compra.Id), c => c.DataVencimento, c => c.NumeroParcela, DateTime.Today);
        foreach (var conta in contas)
        {
            if (restante <= 0) break;
            var abatido = conta.Abater(restante);
            if (abatido <= 0) continue;
            await _contasPagarRepository.AtualizarContaPagar(conta.Id, conta);
            await _devolucoesRepository.CriarAbatimento(new DevolucaoCompraAbatimento(devolucao.Id, conta.Id, abatido));
            restante -= abatido;
        }

        if (restante <= 0) return;

        var data = devolucao.DataDevolucao.Date;
        await _contasReceberRepository.CriarContaReceber(new ContasReceber(
            descricao: $"Reembolso da devolução nº {devolucao.Id} (compra nº {compra.Id})",
            valor: restante,
            cliente: null,
            dataVencimento: data,
            numeroDocumento: $"DEVC-{devolucao.Id}",
            dataEmissao: data,
            origemTipo: OrigemTituloFinanceiro.DEVOLUCAO_COMPRA,
            origemId: devolucao.Id,
            fornecedorId: compra.Fornecedor.Id,
            fornecedorNome: compra.Fornecedor.NomeRazaosocial));
    }

    // Só as parcelas da própria compra (o frete de terceiro é outro título e não é abatido).
    private async Task<IReadOnlyList<ContasPagar>> ObterContasDaCompra(int compraId)
    {
        var todas = new List<ContasPagar>();
        for (var pagina = 1; ; pagina++)
        {
            var r = await _contasPagarRepository.PesquisarContasPagar("", pagina, 100, new FiltroContasPagar(OrigemTipo: OrigemTituloFinanceiro.COMPRA, OrigemId: compraId));
            var antes = todas.Count;
            todas.AddRange(r.Itens);
            if (todas.Count == antes || todas.Count >= r.TotalDeItens) break;
        }
        return todas.Where(c => c.Status != StatusTituloFinanceiro.CANCELADO).ToList();
    }
}
