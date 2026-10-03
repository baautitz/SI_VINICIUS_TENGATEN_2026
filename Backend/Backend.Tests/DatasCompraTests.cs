using Backend.Core.Features.Compras.Services;

namespace Backend.Tests;

public class DatasCompraTests
{
    // 2026-10-02 12:00 em Brasília.
    private static readonly DateTime Agora = new(2026, 10, 2, 15, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Hoje = new(2026, 10, 2);
    private static string? Codigo(DateTime chegada, DateTime? emissao, DateTime? agora = null)
        => DatasCompra.Validar(chegada, emissao, agora ?? Agora)?.Code;

    [Fact] public void Hoje_nao_tem_fuso_e_o_dia_local_dele_e_ele_mesmo() { Assert.Equal(DateTimeKind.Unspecified, DatasCompra.Hoje(Agora).Kind); Assert.Equal(Hoje, DatasCompra.DiaLocal(DatasCompra.Hoje(Agora))); }
    [Fact] public void Hoje_exato_e_valido() => Assert.Null(Codigo(Hoje, Hoje));
    [Fact] public void Emissao_igual_a_chegada_e_valida() => Assert.Null(Codigo(Hoje.AddDays(-3), Hoje.AddDays(-3)));
    [Fact] public void Compra_manual_sem_emissao() => Assert.Null(Codigo(Hoje, null));
    [Fact] public void Chegada_amanha_e_invalida() => Assert.Equal("DATA_CHEGADA_FUTURA", Codigo(Hoje.AddDays(1), Hoje));
    [Fact] public void Chegada_amanha_manual_e_invalida() => Assert.Equal("DATA_CHEGADA_FUTURA", Codigo(Hoje.AddDays(1), null));
    [Fact] public void Emissao_amanha_e_invalida() => Assert.Equal("DATA_EMISSAO_FUTURA", Codigo(Hoje, Hoje.AddDays(1)));
    [Fact] public void Chegada_anterior_a_emissao_e_invalida() => Assert.Equal("DATA_CHEGADA_ANTERIOR_EMISSAO", Codigo(Hoje.AddDays(-2), Hoje.AddDays(-1)));

    [Fact]
    public void Virada_de_dia_usa_horario_de_brasilia_e_nao_utc()
    {
        // 01:00 UTC de 03/10 ainda é 22:00 de 02/10 em Brasília.
        var antes = new DateTime(2026, 10, 3, 1, 0, 0, DateTimeKind.Utc);
        Assert.Equal(Hoje, DatasCompra.Hoje(antes));
        Assert.Null(Codigo(antes, Hoje, antes));
        // O instante da chegada (UTC de 03/10 01:00) é dia 02 local; 03:00 UTC já é dia 03.
        Assert.Equal("DATA_CHEGADA_FUTURA", Codigo(new DateTime(2026, 10, 3, 3, 0, 0, DateTimeKind.Utc), null, antes));
        // Meia-noite local: 03:00 UTC vira o dia.
        Assert.Equal(Hoje.AddDays(1), DatasCompra.Hoje(new DateTime(2026, 10, 3, 3, 0, 0, DateTimeKind.Utc)));
    }
}
