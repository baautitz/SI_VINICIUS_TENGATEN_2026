using System;
using System.Linq;
using Backend.Core.Common.Exceptions;
using Backend.Core.Features.Financeiro.Entities;
using Backend.Core.Features.Financeiro.Entities.Enums;
using Backend.Core.Features.Parceiros.Entities;
using Xunit;

namespace Backend.Tests;

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
