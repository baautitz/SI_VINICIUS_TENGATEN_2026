using System.Reflection;
using Backend.Core.Common.Results;
using Backend.Core.Features.Catalogo.Entities;
using Backend.Core.Features.Compras.Commands;
using Backend.Core.Features.Compras.Entities;
using Backend.Core.Features.Compras.Repositories;
using Backend.Core.Features.Compras.Services;
using Backend.Core.Features.Estoque.Entities;
using Backend.Core.Features.Estoque.Entities.Enums;
using Backend.Core.Features.Estoque.Repositories;
using Backend.Core.Features.Estoque.Services;
using Backend.Core.Features.Financeiro.Entities;
using Backend.Core.Features.Financeiro.Entities.Enums;
using Backend.Core.Features.Financeiro.Repositories;
using Backend.Core.Features.NFe.Entities;
using Backend.Core.Features.NFe.Entities.Enums;
using Backend.Core.Features.NFe.Repositories;
using Backend.Core.Features.Parceiros.Entities;
using Backend.Core.Features.Parceiros.Repositories;
using Backend.Core.Features.Acesso.Repositories;
using Backend.Core.Features.Catalogo.Repositories;
using static Backend.Tests.ComprasServiceTests;

namespace Backend.Tests;

public class ComprasEstornoTests
{
    private class Cenario
    {
        public Skus Sku = new("SKU-1", 10m, estoque: 2, custoMedio: 5m);
        public Compra Compra;
        public ContasPagar Conta;
        public Nfes Nfe;
        public bool Posterior, Estornada, FalhaNaConta;
        public List<MovimentacoesEstoques> MovimentacoesCriadas = new();
        public List<ContasPagar> ContasAtualizadas = new();
        public int NfesCanceladas, Marcadas;
        public UowFake Uow = new();
        public ComprasService Service;

        public Cenario(bool paga = false, bool nfeEmitida = true, bool semUltimaCompra = false)
        {
            var produto = Instancia<Produtos>();
            typeof(Produtos).GetProperty(nameof(Produtos.Produto))!.SetValue(produto, "Produto teste");
            var un = Instancia<UnidadesMedida>();
            typeof(UnidadesMedida).GetProperty(nameof(UnidadesMedida.Sigla))!.SetValue(un, "UN");
            produto.DefinirUnidadeMedida(un);
            Sku.AtualizarProduto(produto);

            Compra = new Compra(DateTime.UtcNow, Instancia<Emitentes>(), Instancia<Fornecedores>(),
                new[] { new CompraItens(2, 5m, 0, Sku) }, 0, 0, 0, 0, 0, null, null) { Id = 7 };

            Conta = new ContasPagar("Compra nº 7", 10m, Instancia<Fornecedores>(), DateTime.UtcNow.AddDays(30), "COMPRA-7", origemTipo: OrigemTituloFinanceiro.COMPRA, origemId: 7);
            if (paga) Conta.RegistrarPagamento(4m);

            Nfe = Instancia<Nfes>();
            typeof(Nfes).GetProperty(nameof(Nfes.TipoOperacao))!.SetValue(Nfe, TipoOperacao.ENTRADA);
            typeof(Nfes).GetProperty(nameof(Nfes.StatusNfe))!.SetValue(Nfe, nfeEmitida ? StatusNfe.EMITIDA : StatusNfe.CANCELADA);

            var entrada = new MovimentacoesEstoques(3, DateTime.UtcNow, TipoMovimentacaoEstoque.ENTRADA, OrigemMovimentacaoEstoque.COMPRA, 7, null, null, null, false);
            entrada.AdicionarItemExistente(new MovimentacoesEstoquesItens(1, 3, Sku, 2, 5m, 0, 0, "p", "UN", semUltimaCompra ? null : 1.5m));

            var movs = Criar<IMovimentacoesEstoquesRepository>(h =>
            {
                h["ObterMovimentacoesPorOrigem"] = _ => new List<MovimentacoesEstoques> { entrada }.AsReadOnly();
                h["ObterMovimentacoesPosteriores"] = _ => (IReadOnlyList<MovimentacaoPosterior>)(Posterior
                    ? new[] { new MovimentacaoPosterior("SKU-1", 9, "ENTRADA", "COMPRA") } : Array.Empty<MovimentacaoPosterior>());
                h["CriarMovimentacao"] = a => { MovimentacoesCriadas.Add((MovimentacoesEstoques)a![0]!); return a[0]; };
            });
            var skus = Criar<ISkusRepository>(h => h["ObterSkuPorSku"] = _ => Sku);

            Service = new ComprasService(
                Criar<IComprasRepository>(h =>
                {
                    h["ObterCompraPorId"] = _ => Compra;
                    h["MarcarEstornada"] = _ => { Marcadas++; return !Estornada; };
                }),
                Criar<IFornecedoresRepository>(), Criar<IEmitentesRepository>(), Criar<ITransportadorasRepository>(), skus,
                Criar<ICondicoesPagamentosRepository>(),
                new MovimentacoesEstoquesService(movs, skus, Criar<IUsuariosRepository>(), Uow),
                Criar<IContasPagarRepository>(h =>
                {
                    h["PesquisarContasPagar"] = _ => new ResultadoPaginado<ContasPagar>(new[] { Conta }, 1, 1, 100);
                    h["AtualizarContaPagar"] = a => { if (FalhaNaConta) throw new InvalidOperationException("falha simulada"); ContasAtualizadas.Add((ContasPagar)a![1]!); return a[1]; };
                }),
                Criar<INfesRepository>(h =>
                {
                    h["ObterNfeDaCompra"] = _ => Nfe;
                    h["CancelarNfe"] = _ => { NfesCanceladas++; return true; };
                }),
                Criar<IUsuariosRepository>(), Uow);
        }
    }

    private static string[] Codigos(Resultado<Compra> r) => r.Errors!.Select(e => e.Code).ToArray();

    [Fact]
    public async Task Estorno_ok_reverte_estoque_cancela_conta_e_nfe_e_commita()
    {
        var c = new Cenario();
        var r = await c.Service.Estornar(7, new EstornarCompraCommand("Compra lançada errada"));

        Assert.True(r.Success);
        Assert.Equal(1, c.Uow.Commits);
        Assert.Equal(0, c.Uow.Rollbacks);
        Assert.Equal(0, c.Sku.Estoque);
        Assert.Equal(0m, c.Sku.CustoMedio); // custo médio de antes da entrada
        Assert.Equal(1.5m, c.Sku.CustoUltimaCompra); // custo da última compra de antes da entrada
        var estorno = Assert.Single(c.MovimentacoesCriadas);
        Assert.Equal(OrigemMovimentacaoEstoque.ESTORNO, estorno.OrigemTipo);
        Assert.Equal(3, estorno.OrigemId);
        Assert.Equal(StatusTituloFinanceiro.CANCELADO, Assert.Single(c.ContasAtualizadas).Status);
        Assert.Equal(StatusTituloFinanceiro.CANCELADO, c.Conta.Status);
        Assert.Equal(StatusNfe.CANCELADA, c.Nfe.StatusNfe);
        Assert.Equal("Compra lançada errada", c.Nfe.JustificativaCancelamento);
        Assert.True(c.Compra.Estornada);
    }

    [Fact]
    public async Task Estorno_de_movimentacao_antiga_mantem_custo_da_ultima_compra()
    {
        var c = new Cenario(semUltimaCompra: true);
        var antes = c.Sku.CustoUltimaCompra;
        var r = await c.Service.Estornar(7, new EstornarCompraCommand("Compra lançada errada"));

        Assert.True(r.Success);
        Assert.Equal(0, c.Sku.Estoque);
        Assert.Equal(0m, c.Sku.CustoMedio);
        Assert.Equal(antes, c.Sku.CustoUltimaCompra); // inalterado
        Assert.Contains("MANTIDO INALTERADO", Assert.Single(c.MovimentacoesCriadas).Observacao);
    }

    [Fact]
    public async Task Condicao_inativa_na_compra_falha()
    {
        var service = new ComprasService(
            Criar<IComprasRepository>(),
            Criar<IFornecedoresRepository>(h => h["ObterFornecedorPorId"] = _ => Instancia<Fornecedores>()),
            Criar<IEmitentesRepository>(h => h["ObterEmitentePorId"] = _ => Instancia<Emitentes>()),
            Criar<ITransportadorasRepository>(), Criar<ISkusRepository>(),
            Criar<ICondicoesPagamentosRepository>(h => h["ObterCondicaoPagamentoPorId"] = _ => Instancia<CondicoesPagamentos>()),
            new MovimentacoesEstoquesService(Criar<IMovimentacoesEstoquesRepository>(), Criar<ISkusRepository>(), Criar<IUsuariosRepository>(), new UowFake()),
            Criar<IContasPagarRepository>(), Criar<INfesRepository>(), Criar<IUsuariosRepository>(), new UowFake());
        var r = await service.GravarCompra(new CriarCompraCommand(
            DateTime.UtcNow, 1, 1, 0, 0, 0, 0, 0, null, null, null,
            new[] { new CriarCompraItemCommand("SKU-1", 2, 5m, 0, null) }, 5, new[] { new Backend.Core.Features.Financeiro.Commands.CriarParcelaPagarCommand(1, DateTime.UtcNow.AddDays(30), 10m) }));
        Assert.Equal("CONDICAO_PAGAMENTO_INATIVA", r.Errors!.First().Code);
    }

    [Fact]
    public async Task Nfe_nao_emitida_nao_e_alterada_e_fica_no_motivo()
    {
        var c = new Cenario(nfeEmitida: false);
        var r = await c.Service.Estornar(7, new EstornarCompraCommand("Compra lançada errada"));

        Assert.True(r.Success);
        Assert.Equal(0, c.NfesCanceladas);
        Assert.Contains("CANCELADA", c.Compra.MotivoEstorno);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    public async Task Motivo_curto_e_recusado_sem_abrir_transacao(string motivo)
    {
        var c = new Cenario();
        var r = await c.Service.Estornar(7, new EstornarCompraCommand(motivo));
        Assert.Equal(new[] { "MOTIVO_INVALIDO" }, Codigos(r));
        Assert.Equal(0, c.Marcadas);
    }

    [Fact]
    public async Task Compra_ja_estornada_e_conflito()
    {
        var c = new Cenario { Estornada = true };
        var r = await c.Service.Estornar(7, new EstornarCompraCommand("primeiro estorno"));
        Assert.Contains(ComprasService.CompraJaEstornada, Codigos(r));
        Assert.True(ComprasService.EhConflito(r.Errors!));
        Assert.Equal(0, c.Uow.Commits);
    }

    [Fact]
    public async Task Parcela_paga_bloqueia_e_nada_e_gravado()
    {
        var c = new Cenario(paga: true);
        var r = await c.Service.Estornar(7, new EstornarCompraCommand("motivo valido"));

        Assert.Contains("ESTORNO_PARCELA_PAGA", Codigos(r));
        Assert.Contains("Estorne o pagamento primeiro", r.Errors!.First().Message);
        Assert.Equal(1, c.Uow.Rollbacks);
        Assert.Equal(0, c.Uow.Commits);
        Assert.Empty(c.MovimentacoesCriadas);
        Assert.Empty(c.ContasAtualizadas);
        Assert.Equal(0, c.NfesCanceladas);
    }

    [Fact]
    public async Task Estoque_menor_que_a_entrada_bloqueia()
    {
        var c = new Cenario();
        c.Sku.AjustarEstoque(-1); // parte já saiu
        var r = await c.Service.Estornar(7, new EstornarCompraCommand("motivo valido"));

        Assert.Equal(new[] { "ESTORNO_ESTOQUE_INSUFICIENTE" }, Codigos(r));
        Assert.Contains("SKU-1", r.Errors!.First().Message);
        Assert.Equal(0, c.Uow.Commits);
        Assert.Empty(c.MovimentacoesCriadas);
    }

    [Fact]
    public async Task Movimentacao_posterior_no_sku_bloqueia()
    {
        var c = new Cenario { Posterior = true };
        var r = await c.Service.Estornar(7, new EstornarCompraCommand("motivo valido"));

        Assert.Equal(new[] { "ESTORNO_MOVIMENTACAO_POSTERIOR" }, Codigos(r));
        Assert.Contains("SKU-1", r.Errors!.First().Message);
        Assert.Equal(1, c.Uow.Rollbacks);
        Assert.Empty(c.MovimentacoesCriadas);
    }

    [Fact]
    public async Task Falha_no_meio_faz_rollback_e_nao_commita()
    {
        var c = new Cenario { FalhaNaConta = true };
        await Assert.ThrowsAsync<InvalidOperationException>(() => c.Service.Estornar(7, new EstornarCompraCommand("motivo valido")));

        Assert.Equal(1, c.Uow.Rollbacks);
        Assert.Equal(0, c.Uow.Commits);
        Assert.Equal(0, c.NfesCanceladas);
    }
}
