using Backend.Core.Common;
using Backend.Core.Common.Exceptions;
using Backend.Core.Common.Extensions;
using Backend.Core.Common.Interfaces;
using Backend.Core.Common.Results;
using Backend.Core.Features.Acesso.Repositories;
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
using Backend.Core.Features.NFe.Repositories;
using Backend.Core.Features.Parceiros.Repositories;

namespace Backend.Core.Features.Compras.Services;

// Limitações conhecidas (fora de escopo): estornar compra já paga exige estornar o pagamento antes e não gera crédito
// com o fornecedor; parcelas canceladas no estorno não têm histórico próprio (só o status CANCELADO); não emite NF-e de
// devolução (a NF-e de entrada é apenas marcada como cancelada); créditos de impostos (ICMS/IPI etc.) não são tratados.
// Contas a pagar: parcela de XML (cobr/dup) nunca nasce paga; só compra manual à vista paga a conta do fornecedor;
// o frete de terceiro nunca é baixado automaticamente.
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
    private readonly INfesRepository _nfesRepository;
    private readonly IUsuariosRepository _usuariosRepository;
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
        INfesRepository nfesRepository,
        IUsuariosRepository usuariosRepository,
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
        _nfesRepository = nfesRepository;
        _usuariosRepository = usuariosRepository;
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
        return await ExecuteResultAsync(async () =>
        {
            try
            {
                _unitOfWork.BeginTransaction();
                var resultado = await GravarCompra(command);
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

    public const string CompraInexistente = "COMPRA_INEXISTENTE";
    public const string CompraJaEstornada = "COMPRA_JA_ESTORNADA";

    // Falha por trava de negócio (HTTP 409); COMPRA_INEXISTENTE é 404 e o resto, 400.
    public static bool EhConflito(IEnumerable<ResultadoErro> erros)
        => erros.Any(e => e.Code is CompraJaEstornada || e.Code.StartsWith("ESTORNO_"));

    // Desfaz a compra numa transação só: estoque (entrada inversa, custo médio anterior), contas a pagar canceladas
    // e NF-e de entrada cancelada. Vínculos fornecedor-SKU permanecem. Com qualquer trava, nada é gravado.
    public async Task<Resultado<Compra>> Estornar(int id, EstornarCompraCommand command)
    {
        var motivo = command?.Motivo?.Trim() ?? "";
        if (motivo.Length < 5 || motivo.Length > 255)
            return Resultado<Compra>.Falha(new ResultadoErro("MOTIVO_INVALIDO", "O motivo do estorno deve ter entre 5 e 255 caracteres.", "motivo"));

        var compra = await _comprasRepository.ObterCompraPorId(id);
        if (compra is null)
            return Resultado<Compra>.Falha(new ResultadoErro(CompraInexistente, "Compra não encontrada."));
        if (compra.Estornada)
            return Resultado<Compra>.Falha(new ResultadoErro(CompraJaEstornada, "Esta compra já foi estornada."));
        if (command!.UsuarioId.HasValue && await _usuariosRepository.ObterUsuarioPorId(command.UsuarioId.Value) is null)
            return Resultado<Compra>.Falha(new ResultadoErro("USUARIO_INEXISTENTE", "O usuário informado não existe.", "UsuarioId"));

        return await ExecuteResultAsync(async () =>
        {
            try
            {
                _unitOfWork.BeginTransaction();
                var resultado = await EstornarNaTransacao(compra, motivo, command.UsuarioId);
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

    private static string Truncar(string v, int max) => v.Length <= max ? v : v[..max];

    private async Task<Resultado<Compra>> EstornarNaTransacao(Compra compra, string motivo, int? usuarioId)
    {
        // Primeiro o UPDATE guardado: trava a linha, então um estorno concorrente espera e cai em JA_ESTORNADA.
        var quando = DateTime.UtcNow;
        // NF-e de entrada que existe mas não está EMITIDA (ex.: já cancelada) não é alterada; fica registrada no motivo (limite de 255).
        var nfe = await _nfesRepository.ObterNfeDaCompra(compra.Id);
        var motivoGravado = nfe is not null && nfe.StatusNfe != NFe.Entities.Enums.StatusNfe.EMITIDA
            ? Truncar($"{motivo} [NF-e {nfe.Numero} estava {nfe.StatusNfe}, não alterada]", 255)
            : motivo;
        compra.Estornar(motivoGravado, usuarioId, quando);
        if (!await _comprasRepository.MarcarEstornada(compra))
            return Resultado<Compra>.Falha(new ResultadoErro(CompraJaEstornada, "Esta compra já foi estornada."));

        // Trava os SKUs antes de checar bloqueios: venda/movimentação concorrente espera o estorno terminar.
        await _skusRepository.TravarSkus(compra.Itens.Select(i => i.Sku.Sku));

        var contas = (await _contasPagarRepository.PesquisarContasPagar("", 1, 100,
            new Financeiro.DTOs.FiltroContasPagar(OrigemTipo: OrigemTituloFinanceiro.COMPRA, OrigemId: compra.Id))).Itens
            .Where(c => c.Status != StatusTituloFinanceiro.CANCELADO).ToList();

        var erros = new List<ResultadoErro>();
        foreach (var conta in contas)
            foreach (var p in conta.ContasPagarParcelas.Where(p => p.ValorPago > 0))
                erros.Add(new ResultadoErro("ESTORNO_PARCELA_PAGA",
                    $"A parcela {p.NumeroParcela} da conta '{conta.Descricao}' já tem R$ {p.ValorPago:0.00} pago. Estorne o pagamento primeiro.", "ContasPagar"));
        erros.AddRange(await _estoqueService.ObterBloqueiosDeEstornoDeEntrada(OrigemMovimentacaoEstoque.COMPRA, compra.Id));
        if (erros.Count > 0)
            return Resultado<Compra>.Falha(erros);

        await _estoqueService.EstornarPorOrigem(OrigemMovimentacaoEstoque.COMPRA, compra.Id, $"Estorno da compra nº {compra.Id}: {motivo}");

        foreach (var conta in contas)
        {
            conta.Cancelar();
            await _contasPagarRepository.AtualizarContaPagar(conta.Id, conta);
        }

        if (nfe is not null && nfe.StatusNfe == NFe.Entities.Enums.StatusNfe.EMITIDA)
        {
            nfe.CancelarPorEstorno(motivo, quando);
            if (!await _nfesRepository.CancelarNfe(nfe))
                throw new DomainException("A NF-e de entrada deixou de estar emitida durante o estorno.");
        }

        return Resultado<Compra>.Sucesso(compra);
    }

    // Grava na transação já aberta pelo chamador (ex.: importação de NF-e, que grava a NFe junto).
    // emissaoNfe != null = compra vinda de XML: título nasce com a emissão da NF-e e as parcelas (cobr/dup) nunca nascem pagas.
    public async Task<Resultado<Compra>> GravarCompra(CriarCompraCommand command, DateTime? emissaoNfe = null)
    {
        var validation = await new CriarCompraCommandValidator().ValidateAsync(command);
        if (!validation.IsValid)
            return Resultado<Compra>.Falha(validation.ToResultadoErros());

        if (DatasCompra.Validar(command.DataCompra, null) is { } erroData)
            return Resultado<Compra>.Falha(erroData);

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
            if (!condicao.Ativo)
                return Resultado<Compra>.Falha(new ResultadoErro("CONDICAO_PAGAMENTO_INATIVA", "A condição de pagamento informada está inativa.", "CondicaoPagamentoId"));
        }

        var itens = new List<CompraItens>();
        foreach (var i in command.Itens)
        {
            var sku = await _skusRepository.ObterSkuPorSku(i.Sku);
            if (sku is null)
                return Resultado<Compra>.Falha(new ResultadoErro("SKU_INEXISTENTE", $"O SKU '{i.Sku}' não existe.", "Itens"));
            itens.Add(new CompraItens(i.Quantidade, i.ValorUnitario, i.ValorDesconto, sku, i.CodigoProdutoFornecedor, i.ValorBruto));
        }

        var compra = new Compra(command.DataCompra, emitente, fornecedor, itens,
            command.ValorFrete, command.ValorSeguro, command.ValorOutrasDespesas, command.ValorDesconto,
            command.ValorFreteTerceiro, command.TransportadoraId, command.CondicaoPagamentoId, command.Observacao);

        var criada = await _comprasRepository.CriarCompra(compra);

        // Entrada de estoque ao custo rateado (todas as despesas); itens do mesmo SKU viram uma linha só.
        var entrada = new MovimentacoesEstoques(
            TipoMovimentacaoEstoque.ENTRADA, OrigemMovimentacaoEstoque.COMPRA, criada.Id,
            observacao: $"Compra nº {criada.Id}");

        foreach (var g in criada.Itens.GroupBy(i => i.Sku.Sku))
        {
            var quantidade = g.Sum(i => i.Quantidade);
            var custo = Math.Round(g.Sum(i => i.ValorTotal + i.ValorRateio) / quantidade, RateioCompra.CasasCusto, MidpointRounding.AwayFromZero);
            entrada.AdicionarItem(g.First().Sku, quantidade, custo);
        }

        var recebimento = await _estoqueService.Registrar(entrada);
        if (!recebimento.Success)
            throw new DomainException(recebimento.Errors!.First().Message);

        foreach (var item in criada.Itens.Where(i => i.CodigoProdutoFornecedor != null))
            await _comprasRepository.VincularSku(fornecedor.Id, item.CodigoProdutoFornecedor!, item.Sku.Sku);

        // Só a compra manual à vista (entrada mínima 100%) baixa a conta do fornecedor; o frete de terceiro nunca é baixado sozinho.
        var aVista = emissaoNfe is null && condicao?.EntradaMinimaPercentual >= 100;
        var conta = new ContasPagar(
            descricao: $"Compra nº {criada.Id}", valorOriginal: criada.ValorNota, fornecedor: fornecedor,
            dataEmissao: emissaoNfe ?? criada.DataCompra, condicaoPagamento: condicao,
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

        return Resultado<Compra>.Sucesso(criada);
    }
}
