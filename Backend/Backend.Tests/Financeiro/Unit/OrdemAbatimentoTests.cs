using Backend.Core.Features.Financeiro.Policies;
using Xunit;

namespace Backend.Tests.Financeiro.Unit;

public class OrdemAbatimentoTests
{
    private static readonly DateTime Hoje = new(2026, 6, 15);

    private static string Ordem(params (int Parcela, int DiasParaVencer)[] parcelas)
        => string.Join(",", OrdemAbatimento
            .Ordenar(parcelas, p => Hoje.AddDays(p.DiasParaVencer), p => p.Parcela, Hoje)
            .Select(p => p.Parcela));

    [Fact]
    public void Vencidas_vem_primeiro_da_mais_antiga_para_a_mais_recente()
        => Assert.Equal("1,2,3", Ordem((3, -5), (1, -40), (2, -20)));

    [Fact]
    public void A_vencer_segue_da_ultima_para_a_primeira()
        => Assert.Equal("3,2,1", Ordem((1, 10), (2, 40), (3, 70)));

    [Fact]
    public void Vencidas_antes_das_a_vencer()
        => Assert.Equal("1,4,3,2", Ordem((1, -10), (2, 30), (3, 60), (4, -1)));

    [Fact]
    public void Parcela_que_vence_hoje_ainda_nao_esta_vencida()
        => Assert.Equal("1,2", Ordem((2, 0), (1, -1)));

    [Fact]
    public void Vencidas_no_mesmo_dia_desempatam_pelo_numero_da_parcela()
        => Assert.Equal("1,2", Ordem((2, -7), (1, -7)));
}
