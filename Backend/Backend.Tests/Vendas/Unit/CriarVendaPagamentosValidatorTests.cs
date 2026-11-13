using Backend.Core.Features.Financeiro.Commands;
using Backend.Core.Features.Vendas.Commands;
using Backend.Core.Features.Vendas.Validators.Commands;

namespace Backend.Tests.Vendas.Unit;

public class CriarVendaPagamentosValidatorTests
{
    private static CriarParcelaReceberCommand Parcela(int n, decimal valor) => new(n, DateTime.UtcNow.AddDays(30 * n), valor);

    private static CriarVendaCommand Venda(params CriarVendaPagamentoCommand[] pagamentos) =>
        new(DateTime.UtcNow, 1, 1, null, new[] { new CriarVendaItemCommand("SKU", 10, 10, 0) }, pagamentos);

    [Fact]
    public void Aceita_soma_dos_pagamentos_igual_ao_total()
    {
        var r = new CriarVendaCommandValidator().Validate(Venda(
            new CriarVendaPagamentoCommand(1, 40, new[] { Parcela(1, 40) }),
            new CriarVendaPagamentoCommand(2, 60, new[] { Parcela(1, 30), Parcela(2, 30) })));
        Assert.True(r.IsValid, string.Join("; ", r.Errors.Select(e => e.ErrorMessage)));
    }

    [Fact]
    public void Rejeita_soma_diferente_do_total()
    {
        var r = new CriarVendaCommandValidator().Validate(Venda(
            new CriarVendaPagamentoCommand(1, 40, new[] { Parcela(1, 40) }),
            new CriarVendaPagamentoCommand(2, 50, new[] { Parcela(1, 50) })));
        Assert.Contains(r.Errors, e => e.ErrorMessage.Contains("soma das formas de pagamento"));
    }

    [Fact]
    public void Tolera_diferenca_de_menos_de_um_centavo()
    {
        var r = new CriarVendaCommandValidator().Validate(Venda(
            new CriarVendaPagamentoCommand(1, 99.995m, new[] { Parcela(1, 99.995m) })));
        Assert.True(r.IsValid);
    }

    [Fact]
    public void Rejeita_valor_zero_e_lista_vazia()
    {
        var zero = new CriarVendaCommandValidator().Validate(Venda(
            new CriarVendaPagamentoCommand(1, 0, new[] { Parcela(1, 100) }),
            new CriarVendaPagamentoCommand(2, 100, new[] { Parcela(1, 100) })));
        Assert.Contains(zero.Errors, e => e.ErrorMessage.Contains("maior que zero"));

        var vazia = new CriarVendaCommandValidator().Validate(Venda());
        Assert.Contains(vazia.Errors, e => e.ErrorMessage.Contains("ao menos uma forma de pagamento"));
    }

    [Fact]
    public void Rejeita_parcelas_que_nao_somam_o_valor_da_linha()
    {
        var r = new CriarVendaCommandValidator().Validate(Venda(
            new CriarVendaPagamentoCommand(1, 100, new[] { Parcela(1, 40), Parcela(2, 40) })));
        Assert.Contains(r.Errors, e => e.ErrorMessage.Contains("soma das parcelas"));
    }
}
