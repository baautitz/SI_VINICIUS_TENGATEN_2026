using Backend.Core.Features.Financeiro.Commands;
using Backend.Core.Features.Financeiro.Entities;
using Backend.Core.Features.Financeiro.Repositories;
using Backend.Core.Features.Financeiro.Services;
using Backend.Core.Features.Parceiros.Repositories;
using Backend.Tests.Common;
using static Backend.Tests.Common.Fakes;

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

    private static (ContasReceberService Service, Func<int> Deletadas) ServicoCom(ContasReceber conta)
    {
        var deletadas = 0;
        var service = new ContasReceberService(
            Criar<IContasReceberRepository>(h =>
            {
                h["ObterContaReceberPorIdParaAtualizar"] = _ => conta;
                h["DeletarContaReceber"] = _ => { deletadas++; return true; };
            }),
            Criar<IClientesRepository>(), Criar<ICondicoesPagamentosRepository>(), new UowFake());
        return (service, () => deletadas);
    }

    private static ContasReceber NovaConta()
        => new("Teste", 100m, Instancia<Backend.Core.Features.Parceiros.Entities.Clientes>(), DateTime.UtcNow.AddDays(5), "DOC-1");

    [Fact]
    public async Task Excluir_conta_com_recebimento_parcial_e_recusado()
    {
        var conta = NovaConta();
        conta.RegistrarRecebimento(40m);
        var (service, deletadas) = ServicoCom(conta);

        await Assert.ThrowsAsync<Backend.Core.Common.Exceptions.DomainException>(() => service.DeletarContaReceber(1));
        Assert.Equal(0, deletadas());
    }

    [Fact]
    public async Task Excluir_conta_quitada_e_recusada()
    {
        var conta = NovaConta();
        conta.RegistrarRecebimento(100m);
        var (service, deletadas) = ServicoCom(conta);

        await Assert.ThrowsAsync<Backend.Core.Common.Exceptions.DomainException>(() => service.DeletarContaReceber(1));
        Assert.Equal(0, deletadas());
    }

    [Fact]
    public async Task Excluir_conta_so_com_abatimento_e_recusada()
    {
        var conta = NovaConta();
        conta.Abater(30m);
        var (service, deletadas) = ServicoCom(conta);

        await Assert.ThrowsAsync<Backend.Core.Common.Exceptions.DomainException>(() => service.DeletarContaReceber(1));
        Assert.Equal(0, deletadas());
    }

    [Fact]
    public async Task Excluir_conta_aberta_manual_e_sem_baixa_e_permitido()
    {
        var (service, deletadas) = ServicoCom(NovaConta());

        Assert.True(await service.DeletarContaReceber(1));
        Assert.Equal(1, deletadas());
    }
}
