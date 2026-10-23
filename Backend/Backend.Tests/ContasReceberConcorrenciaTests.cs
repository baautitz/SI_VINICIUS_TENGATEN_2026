using Backend.Core.Features.Financeiro.Commands;
using Backend.Core.Features.Financeiro.Entities;
using Backend.Core.Features.Financeiro.Repositories;
using Backend.Core.Features.Financeiro.Services;
using Backend.Core.Features.Parceiros.Repositories;
using static Backend.Tests.ComprasServiceTests;

namespace Backend.Tests;

public class ContasReceberConcorrenciaTests
{
    // Sem banco não dá para provar o lock; garante que a leitura é a travada (FOR UPDATE) e acumula sobre o valor_pago lido.
    [Fact]
    public async Task Recebimento_le_com_lock_e_acumula_sobre_valor_lido()
    {
        var conta = new ContasReceber("Teste", 120m, Instancia<Backend.Core.Features.Parceiros.Entities.Clientes>(), DateTime.UtcNow.AddDays(5), "DOC-1");
        var lidaComLock = 0;
        ContasReceber? gravada = null;
        var service = new ContasReceberService(
            Criar<IContasReceberRepository>(h =>
            {
                h["ObterContaReceberPorId"] = _ => throw new InvalidOperationException("leitura sem lock");
                h["ObterContaReceberPorIdParaAtualizar"] = _ => { lidaComLock++; return conta; };
                h["AtualizarContaReceber"] = a => { gravada = (ContasReceber)a![1]!; return a[1]; };
            }),
            Criar<IClientesRepository>(), Criar<ICondicoesPagamentosRepository>(), new UowFake());

        var r = await service.RegistrarRecebimento(1, new ReceberContaReceberCommand(30m));

        Assert.True(r.Success);
        Assert.Equal(1, lidaComLock);
        Assert.Equal(30m, gravada!.ValorPago);
    }
}
