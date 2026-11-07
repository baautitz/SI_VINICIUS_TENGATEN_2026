using System;
using System.Linq;
using Backend.Core.Common.Exceptions;
using Backend.Core.Features.Financeiro.Entities;
using Backend.Core.Features.Financeiro.Entities.Enums;
using Backend.Core.Features.Parceiros.Entities;
using Xunit;

namespace Backend.Tests.Financeiro.Unit;

public class ContasPagarTests
{
    [Fact]
    public void Parcelas_do_documento_pagam_parcial_e_cancelam_por_documento()
    {
        var fornecedor = (Fornecedores)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Fornecedores));
        var parcelas = Enumerable.Range(1, 3)
            .Select(i => new ContasPagar("Aluguel", 100m, fornecedor, DateTime.Today.AddDays(30 * i), "DOC-1", i, 3))
            .ToList();

        parcelas[0].RegistrarPagamento(40m);
        Assert.Equal(StatusTituloFinanceiro.PARCIAL, parcelas[0].Status);
        Assert.Equal(60m, parcelas[0].ValorSaldo);
        parcelas[0].RegistrarPagamento(60m);
        Assert.Equal(StatusTituloFinanceiro.PAGO, parcelas[0].Status);

        Assert.Throws<DomainException>(() => parcelas[0].Cancelar());
        foreach (var p in parcelas.Skip(1)) p.Cancelar();
        Assert.All(parcelas.Skip(1), p => Assert.Equal(StatusTituloFinanceiro.CANCELADO, p.Status));
    }

    private static ContasPagar Parcela(decimal valor = 100m)
        => new("Compra", valor, (Fornecedores)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Fornecedores)), DateTime.Today, "DOC-3");

    [Fact]
    public void Abatimento_consome_so_o_saldo_em_aberto_sem_mexer_no_valor_nem_no_pago()
    {
        var c = Parcela();
        c.RegistrarPagamento(30m);

        Assert.Equal(70m, c.Abater(500m));

        Assert.Equal(100m, c.Valor);
        Assert.Equal(30m, c.ValorPago);
        Assert.Equal(70m, c.ValorAbatido);
        Assert.Equal(0m, c.ValorSaldo);
        Assert.Equal(StatusTituloFinanceiro.PAGO, c.Status);
        Assert.True(c.PossuiBaixa);
    }

    [Fact]
    public void Abatimento_parcial_deixa_a_parcela_parcial_e_o_pagamento_respeita_o_saldo()
    {
        var c = Parcela();

        Assert.Equal(40m, c.Abater(40m));

        Assert.Equal(StatusTituloFinanceiro.PARCIAL, c.Status);
        Assert.Equal(60m, c.ValorSaldo);
        Assert.Throws<DomainException>(() => c.RegistrarPagamento(61m));
    }

    [Fact]
    public void Restituir_desfaz_o_abatimento_e_volta_a_parcela_para_aberta()
    {
        var c = Parcela();
        c.Abater(40m);

        Assert.Equal(40m, c.Restituir(40m));

        Assert.Equal(0m, c.ValorAbatido);
        Assert.Equal(StatusTituloFinanceiro.ABERTO, c.Status);
        Assert.False(c.PossuiBaixa);
    }

    [Fact]
    public void Parcela_abatida_nao_pode_ser_cancelada()
        => Assert.Throws<DomainException>(() => { var c = Parcela(); c.Abater(10m); c.Cancelar(); });

    [Fact]
    public void Valor_e_arredondado_em_duas_casas_como_o_banco()
    {
        var fornecedor = (Fornecedores)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Fornecedores));
        var c = new ContasPagar("Aluguel", 10.005m, fornecedor, DateTime.Today, "DOC-2");
        Assert.Equal(10.01m, c.Valor);
        c.Atualizar("Aluguel", DateTime.Today, 20.005m, null);
        Assert.Equal(20.01m, c.Valor);
    }
}
