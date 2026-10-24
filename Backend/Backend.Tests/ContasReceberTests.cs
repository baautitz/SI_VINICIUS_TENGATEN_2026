using System;
using System.Linq;
using Backend.Core.Common.Exceptions;
using Backend.Core.Features.Financeiro.Entities;
using Backend.Core.Features.Financeiro.Entities.Enums;
using Backend.Core.Features.Parceiros.Entities;
using Xunit;

namespace Backend.Tests;

public class ContasReceberTests
{
    private static readonly Clientes Cliente = (Clientes)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Clientes));

    [Fact]
    public void Parcelas_do_documento_recebem_parcial_e_cancelam_por_documento()
    {
        var parcelas = Enumerable.Range(1, 3)
            .Select(i => new ContasReceber("Venda", 100m, Cliente, DateTime.Today.AddDays(30 * i), "VENDA-1", i, 3))
            .ToList();

        parcelas[0].RegistrarRecebimento(40m);
        Assert.Equal(StatusTituloFinanceiro.PARCIAL, parcelas[0].Status);
        Assert.Equal(60m, parcelas[0].ValorSaldo);
        parcelas[0].RegistrarRecebimento(60m);
        Assert.Equal(StatusTituloFinanceiro.PAGO, parcelas[0].Status);

        Assert.Throws<DomainException>(() => parcelas[0].Cancelar());
        foreach (var p in parcelas.Skip(1)) p.Cancelar();
        Assert.All(parcelas.Skip(1), p => Assert.Equal(StatusTituloFinanceiro.CANCELADO, p.Status));
    }

    [Fact]
    public void Abater_incrementa_abatido_sem_mudar_valor_e_devolve_o_abatido()
    {
        var parcial = new ContasReceber("Venda", 100m, Cliente, DateTime.Today, "VENDA-1");
        parcial.RegistrarRecebimento(30m);

        Assert.Equal(70m, parcial.Abater(500m));
        Assert.Equal(100m, parcial.Valor);
        Assert.Equal(30m, parcial.ValorPago);
        Assert.Equal(70m, parcial.ValorAbatido);
        Assert.Equal(0m, parcial.ValorSaldo);
        Assert.Equal(StatusTituloFinanceiro.PAGO, parcial.Status);

        var aberta = new ContasReceber("Venda", 100m, Cliente, DateTime.Today, "VENDA-1");
        Assert.Equal(40m, aberta.Abater(40m));
        Assert.Equal(StatusTituloFinanceiro.PARCIAL, aberta.Status);
        Assert.Equal(60m, aberta.ValorSaldo);
        Assert.Equal(60m, aberta.Abater(100m));
        Assert.Equal(StatusTituloFinanceiro.PAGO, aberta.Status);
        Assert.Equal(100m, aberta.Valor);
        Assert.Equal(0m, aberta.Abater(10m));
    }

    [Fact]
    public void Restituir_decrementa_abatido_recalcula_status_e_nao_mexe_no_pago()
    {
        var c = new ContasReceber("Venda", 100m, Cliente, DateTime.Today, "VENDA-1");
        c.RegistrarRecebimento(50m);
        c.Abater(50m);

        Assert.Equal(20m, c.Restituir(20m));
        Assert.Equal(StatusTituloFinanceiro.PARCIAL, c.Status);
        Assert.Equal(30m, c.ValorAbatido);
        Assert.Equal(50m, c.ValorPago);
        Assert.Equal(30m, c.Restituir(500m));
        Assert.Equal(StatusTituloFinanceiro.PARCIAL, c.Status);
        Assert.Equal(0m, c.ValorAbatido);

        var so = new ContasReceber("Venda", 100m, Cliente, DateTime.Today, "VENDA-1");
        so.Abater(100m);
        so.Restituir(100m);
        Assert.Equal(StatusTituloFinanceiro.ABERTO, so.Status);
    }

    [Fact]
    public void Abatimento_e_baixa_barra_cancelar_e_atualizar()
    {
        var manual = new ContasReceber("Aluguel", 100m, Cliente, DateTime.Today, "MAN-1");
        manual.Abater(10m);

        Assert.True(manual.PossuiBaixa);
        Assert.Throws<DomainException>(() => manual.Cancelar());
        Assert.Throws<DomainException>(() => manual.Atualizar("X", DateTime.Today, 50m, null));
    }

    [Fact]
    public void Recebimento_respeita_saldo_descontado_o_abatido()
    {
        var c = new ContasReceber("Venda", 100m, Cliente, DateTime.Today, "VENDA-1");
        c.Abater(60m);

        Assert.Throws<DomainException>(() => c.RegistrarRecebimento(50m));
        c.RegistrarRecebimento(40m);
        Assert.Equal(StatusTituloFinanceiro.PAGO, c.Status);
        c.EstornarRecebimento(40m);
        Assert.Equal(StatusTituloFinanceiro.PARCIAL, c.Status);
    }

    [Fact]
    public void Estorno_nao_excede_o_recebido_e_edicao_exige_manual_aberta_sem_recebimento()
    {
        var manual = new ContasReceber("Aluguel", 100m, Cliente, DateTime.Today, "MAN-1");
        manual.RegistrarRecebimento(10m);
        Assert.Throws<DomainException>(() => manual.EstornarRecebimento(20m));
        Assert.Throws<DomainException>(() => manual.Atualizar("X", DateTime.Today, 50m, null));
        manual.EstornarRecebimento(10m);
        manual.Atualizar("X", DateTime.Today, 50m, null);
        Assert.Equal(50m, manual.Valor);

        var venda = new ContasReceber("Venda", 100m, Cliente, DateTime.Today, "VENDA-1", origemTipo: OrigemTituloFinanceiro.VENDA, origemId: 1);
        Assert.Throws<DomainException>(() => venda.Atualizar("X", DateTime.Today, 50m, null));
    }

    [Fact]
    public void Valor_e_arredondado_em_duas_casas_como_o_banco()
    {
        var c = new ContasReceber("Venda", 10.005m, Cliente, DateTime.Today, "VENDA-2");
        Assert.Equal(10.01m, c.Valor);
        c.Atualizar("Venda", DateTime.Today, 20.005m, null);
        Assert.Equal(20.01m, c.Valor);
    }

    [Fact]
    public void Baixa_parcial_barra_cancelar_e_atualizar()
    {
        var conta = new ContasReceber("Manual", 100m, Cliente, DateTime.Today.AddDays(30), "MAN-1");
        Assert.False(conta.PossuiBaixa);

        conta.RegistrarRecebimento(10m);

        Assert.True(conta.PossuiBaixa);
        Assert.Throws<DomainException>(() => conta.Cancelar());
        Assert.Throws<DomainException>(() => conta.Atualizar("Outra", DateTime.Today.AddDays(40), 120m, null));
        Assert.Equal(StatusTituloFinanceiro.PARCIAL, conta.Status);
    }
}
