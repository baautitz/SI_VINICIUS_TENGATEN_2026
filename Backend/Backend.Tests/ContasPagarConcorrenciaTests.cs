using Backend.Core.Features.Financeiro.Commands;
using Backend.Core.Features.Financeiro.Entities;
using Backend.Core.Features.Financeiro.Repositories;
using Backend.Core.Features.Financeiro.Services;
using Backend.Core.Features.Parceiros.Repositories;
using static Backend.Tests.ComprasServiceTests;

namespace Backend.Tests;

public class ContasPagarConcorrenciaTests
{
    // Sem banco não dá para provar o lock; garante que a leitura é a travada (FOR UPDATE) e acumula sobre o valor_pago lido.
    [Fact]
    public async Task Pagamento_le_com_lock_e_acumula_sobre_valor_lido()
    {
        var conta = new ContasPagar("Teste", 120m, Instancia<Backend.Core.Features.Parceiros.Entities.Fornecedores>(), DateTime.UtcNow.AddDays(5), "DOC-1");
        var lidaComLock = 0;
        ContasPagar? gravada = null;
        var service = new ContasPagarService(
            Criar<IContasPagarRepository>(h =>
            {
                h["ObterContaPagarPorId"] = _ => throw new InvalidOperationException("leitura sem lock");
                h["ObterContaPagarPorIdParaAtualizar"] = _ => { lidaComLock++; return conta; };
                h["AtualizarContaPagar"] = a => { gravada = (ContasPagar)a![1]!; return a[1]; };
            }),
            Criar<IFornecedoresRepository>(), Criar<ICondicoesPagamentosRepository>(), new UowFake());

        var r = await service.RegistrarPagamento(1, new PagarContaPagarCommand(30m));

        Assert.True(r.Success);
        Assert.Equal(1, lidaComLock);
        Assert.Equal(30m, gravada!.ValorPago);
    }
}
