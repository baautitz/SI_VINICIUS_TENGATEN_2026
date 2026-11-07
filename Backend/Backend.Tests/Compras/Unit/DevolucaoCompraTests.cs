using Backend.Core.Features.Acesso.Repositories;
using Backend.Core.Features.Catalogo.Entities;
using Backend.Core.Features.Catalogo.Repositories;
using Backend.Core.Features.Compras.Commands;
using Backend.Core.Features.Compras.Entities;
using Backend.Core.Features.Compras.Repositories;
using Backend.Core.Features.Compras.Services;
using Backend.Core.Features.Estoque.Entities;
using Backend.Core.Features.Estoque.Entities.Enums;
using Backend.Core.Features.Estoque.Repositories;
using Backend.Core.Features.Estoque.Services;
using Backend.Core.Features.Financeiro.DTOs;
using Backend.Core.Features.Financeiro.Entities;
using Backend.Core.Features.Financeiro.Entities.Enums;
using Backend.Core.Features.Financeiro.Repositories;
using Backend.Core.Features.NFe.Repositories;
using Backend.Core.Features.NFe.Services;
using Backend.Core.Features.Parceiros.Entities;
using Backend.Core.Features.Parceiros.Repositories;
using Backend.Tests.Common;
using static Backend.Tests.Common.Fakes;

namespace Backend.Tests;

public class DevolucaoCompraTests
{
    // Compra 7 com um item (id 11): 10 un a 5,00. O estoque do SKU começa igual ao comprado, salvo o que o teste vender.
    private class Cenario
    {
        public Skus Sku;
        public Compra Compra;
        public List<DevolucaoCompra> Criadas = new();
        public List<MovimentacoesEstoques> Movimentacoes = new();
        public List<ContasPagar> Parcelas = new();
        public List<ContasReceber> Reembolsos = new();
        public List<DevolucaoCompraAbatimento> Abatimentos = new();
        public UowFake Uow = new();
        public DevolucoesCompraService Service;

        public Cenario(decimal estoque = 10, decimal jaDevolvido = 0, bool estornada = false)
        {
            Sku = new Skus("SKU-1", 10m, estoque: estoque, custoMedio: 5m, custoUltimaCompra: 5m);
            var produto = Instancia<Produtos>();
            typeof(Produtos).GetProperty(nameof(Produtos.Produto))!.SetValue(produto, "Produto teste");
            var un = Instancia<UnidadesMedida>();
            typeof(UnidadesMedida).GetProperty(nameof(UnidadesMedida.Sigla))!.SetValue(un, "UN");
            produto.DefinirUnidadeMedida(un);
            Sku.AtualizarProduto(produto);

            var item = new CompraItens(10, 5m, 0, Sku) { Id = 11 };
            typeof(CompraItens).GetProperty(nameof(CompraItens.QuantidadeDevolvida))!.SetValue(item, jaDevolvido);
            Compra = new Compra(DateTime.UtcNow, Instancia<Emitentes>(), Instancia<Fornecedores>(),
                new[] { item }, 0, 0, 0, 0, 0, null, null) { Id = 7 };
            if (estornada) Compra.Estornar("compra lancada errada", null);

            var skus = Criar<ISkusRepository>(h => h["ObterSkuPorSku"] = _ => Sku);
            var movs = Criar<IMovimentacoesEstoquesRepository>(h =>
                h["CriarMovimentacao"] = a => { Movimentacoes.Add((MovimentacoesEstoques)a![0]!); return a[0]; });

            var nfes = Criar<INfesRepository>(); // compra sem NF-e ativa: nada a emitir
            Service = new DevolucoesCompraService(
                Criar<IComprasRepository>(h => h["ObterCompraPorId"] = a => (int)a![0]! == 7 ? Compra : null),
                Criar<IDevolucoesCompraRepository>(h =>
                {
                    h["CriarDevolucao"] = a =>
                    {
                        var d = (DevolucaoCompra)a![0]!;
                        d.Id = 3;
                        Criadas.Add(d);
                        return d;
                    };
                    h["CriarAbatimento"] = a => { Abatimentos.Add((DevolucaoCompraAbatimento)a![0]!); return null; };
                }),
                skus,
                new MovimentacoesEstoquesService(movs, skus, Criar<IUsuariosRepository>(), Uow),
                Criar<IContasPagarRepository>(h =>
                {
                    h["PesquisarContasPagar"] = _ => new Backend.Core.Common.Results.ResultadoPaginado<ContasPagar>(Parcelas, Parcelas.Count, 1, 100);
                    h["AtualizarContaPagar"] = a => (ContasPagar)a![1]!;
                }),
                Criar<IContasReceberRepository>(h => h["CriarContaReceber"] = a => { Reembolsos.Add((ContasReceber)a![0]!); return a[0]; }),
                nfes,
                new NfesDevolucaoService(nfes, Criar<IEmitentesRepository>(), Criar<IClientesRepository>(), Criar<IFornecedoresRepository>()),
                Uow);
        }

        public Task<Backend.Core.Common.Results.Resultado<DevolucaoCompra>> Devolver(decimal quantidade, string motivo = "Produto veio com defeito", int compraId = 7, int itemId = 11)
            => Service.CriarDevolucao(compraId, new CriarDevolucaoCompraCommand(motivo, new[] { new CriarDevolucaoCompraItemCommand(itemId, quantidade) }));
    }

    private static string[] Codigos(Backend.Core.Common.Results.Resultado<DevolucaoCompra> r) => r.Errors!.Select(e => e.Code).ToArray();

    [Fact]
    public async Task Devolucao_parcial_registra_motivo_da_saida_de_estoque_e_commita()
    {
        var c = new Cenario();
        var r = await c.Devolver(4);

        Assert.True(r.Success);
        Assert.Equal(1, c.Uow.Commits);
        var devolucao = Assert.Single(c.Criadas);
        Assert.Equal("Produto veio com defeito", devolucao.Motivo);
        Assert.Equal(7, devolucao.CompraId);
        Assert.Equal(20m, devolucao.ValorTotal);
        var item = Assert.Single(devolucao.Itens);
        Assert.Equal((11, "SKU-1", 4m, 5m), (item.CompraItemId, item.Sku, item.Quantidade, item.CustoUnitario));

        Assert.Equal(6, c.Sku.Estoque);
        var saida = Assert.Single(c.Movimentacoes);
        Assert.Equal(TipoMovimentacaoEstoque.SAIDA, saida.TipoMovimentacao);
        Assert.Equal(OrigemMovimentacaoEstoque.DEVOLUCAO_COMPRA, saida.OrigemTipo);
        Assert.Equal(3, saida.OrigemId);
    }

    private static ContasPagar ParcelaPagar(int numero, decimal valor, int diasParaVencer, decimal pago = 0)
    {
        var f = Instancia<Fornecedores>();
        var conta = new ContasPagar("Compra nº 7", valor, f, DateTime.Today.AddDays(diasParaVencer), "COMPRA-7", numero, 2, origemTipo: OrigemTituloFinanceiro.COMPRA, origemId: 7);
        if (pago > 0) conta.RegistrarPagamento(pago);
        return conta;
    }

    [Fact]
    public async Task Devolucao_abate_o_aberto_da_parcela_vencida_primeiro_sem_gerar_reembolso()
    {
        var c = new Cenario();
        c.Parcelas.AddRange(new[] { ParcelaPagar(1, 30m, -5), ParcelaPagar(2, 30m, 30) });

        var r = await c.Devolver(4); // R$ 20

        Assert.True(r.Success);
        Assert.Equal(20m, c.Parcelas[0].ValorAbatido);
        Assert.Equal(0m, c.Parcelas[1].ValorAbatido);
        var ab = Assert.Single(c.Abatimentos);
        Assert.Equal((3, 20m), (ab.DevolucaoCompraId, ab.Valor));
        Assert.Empty(c.Reembolsos);
    }

    [Fact]
    public async Task Excedente_do_aberto_vira_reembolso_e_o_valor_pago_nunca_e_consumido()
    {
        var c = new Cenario();
        c.Parcelas.AddRange(new[] { ParcelaPagar(1, 30m, 10, pago: 25m), ParcelaPagar(2, 10m, 40) });

        var r = await c.Devolver(10); // R$ 50: abate 5 + 10, reembolsa 35

        Assert.True(r.Success);
        Assert.Equal(25m, c.Parcelas[0].ValorPago);
        Assert.Equal(5m, c.Parcelas[0].ValorAbatido);
        Assert.Equal(10m, c.Parcelas[1].ValorAbatido);
        Assert.Equal(15m, c.Abatimentos.Sum(a => a.Valor));
        var reembolso = Assert.Single(c.Reembolsos);
        Assert.Equal(35m, reembolso.Valor);
        Assert.Equal(OrigemTituloFinanceiro.DEVOLUCAO_COMPRA, reembolso.OrigemTipo);
        Assert.Equal(3, reembolso.OrigemId);
        Assert.Null(reembolso.Cliente);
        Assert.Equal(DateTime.UtcNow.Date, reembolso.DataVencimento.Date);
    }

    [Fact]
    public async Task Compra_toda_paga_vira_so_reembolso()
    {
        var c = new Cenario();
        c.Parcelas.Add(ParcelaPagar(1, 50m, -1, pago: 50m));

        var r = await c.Devolver(4);

        Assert.True(r.Success);
        Assert.Empty(c.Abatimentos);
        Assert.Equal(20m, Assert.Single(c.Reembolsos).Valor);
    }

    [Fact]
    public async Task Devolucao_corrige_o_custo_medio_tirando_a_contribuicao_dos_itens_devolvidos()
    {
        // Estoque de 10 un a 5,80 (6 un a 3,00 antes da compra de 4 un a 10,00): devolvendo as 4 un volta a 3,00.
        var c = new Cenario();
        typeof(Skus).GetProperty(nameof(Skus.CustoMedio))!.SetValue(c.Sku, 5.8m);
        c.Compra.Itens.First().GetType().GetProperty(nameof(CompraItens.CustoUnitario))!.SetValue(c.Compra.Itens.First(), 10m);

        var r = await c.Devolver(4);

        Assert.True(r.Success);
        Assert.Equal(3m, c.Sku.CustoMedio);
    }

    [Fact]
    public async Task Quantidade_acima_do_saldo_em_estoque_e_recusada_com_mensagem_clara_e_nada_e_gravado()
    {
        var c = new Cenario(estoque: 3); // 7 un da compra já foram vendidas
        var r = await c.Devolver(4);

        Assert.Equal(new[] { "DEVOLUCAO_ACIMA_DO_ESTOQUE" }, Codigos(r));
        Assert.Contains("SKU-1", r.Errors!.First().Message);
        Assert.Contains("3", r.Errors!.First().Message);
        Assert.Equal(1, c.Uow.Rollbacks);
        Assert.Equal(0, c.Uow.Commits);
        Assert.Empty(c.Criadas);
        Assert.Empty(c.Movimentacoes);
        Assert.Equal(3, c.Sku.Estoque);
    }

    [Fact]
    public async Task Devolucao_total_do_saldo_e_aceita_e_depois_nao_ha_mais_saldo()
    {
        var c = new Cenario();
        var total = await c.Devolver(10);
        Assert.True(total.Success);
        Assert.Equal(0, c.Sku.Estoque);

        // Repositório reflete o que já foi devolvido: o saldo devolvível do item zera.
        var item = c.Compra.Itens.First();
        typeof(CompraItens).GetProperty(nameof(CompraItens.QuantidadeDevolvida))!.SetValue(item, 10m);
        Assert.Equal(0, item.SaldoDevolvivel);

        var outra = await c.Devolver(1);
        Assert.Equal(new[] { "DEVOLUCAO_ACIMA_DO_SALDO" }, Codigos(outra));
    }

    [Fact]
    public async Task Quantidade_acima_do_saldo_devolvivel_do_item_e_recusada()
    {
        var c = new Cenario(estoque: 10, jaDevolvido: 8); // ainda restam 2 a devolver
        var r = await c.Devolver(3);

        Assert.Equal(new[] { "DEVOLUCAO_ACIMA_DO_SALDO" }, Codigos(r));
        Assert.Contains("2", r.Errors!.First().Message);
        Assert.Empty(c.Criadas);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    public async Task Motivo_curto_e_recusado(string motivo)
    {
        var c = new Cenario();
        var r = await c.Devolver(1, motivo);
        Assert.Contains("MOTIVO_INVALIDO", Codigos(r));
        Assert.Empty(c.Criadas);
    }

    [Fact]
    public async Task Motivo_acima_de_255_caracteres_e_recusado()
    {
        var c = new Cenario();
        var r = await c.Devolver(1, new string('a', 256));
        Assert.Contains("MOTIVO_INVALIDO", Codigos(r));
        Assert.Empty(c.Criadas);
    }

    [Fact]
    public async Task Sem_quantidade_a_devolver_e_recusado()
    {
        var c = new Cenario();
        var r = await c.Devolver(0);
        Assert.Equal(new[] { "ITENS_OBRIGATORIOS" }, Codigos(r));
    }

    [Fact]
    public async Task Compra_inexistente_item_de_outra_compra_e_compra_estornada_sao_recusados()
    {
        Assert.Equal(new[] { "COMPRA_INEXISTENTE" }, Codigos(await new Cenario().Devolver(1, compraId: 99)));
        Assert.Equal(new[] { "ITEM_INEXISTENTE" }, Codigos(await new Cenario().Devolver(1, itemId: 55)));
        Assert.Equal(new[] { "COMPRA_ESTORNADA" }, Codigos(await new Cenario(estornada: true).Devolver(1)));
    }
}
