using Backend.Core.Common.Helpers;

namespace Backend.Tests.Parceiros.Unit;

public class PlacaVeiculoTests
{
    [Theory]
    [InlineData("ABC1234")]
    [InlineData("abc-1234")]
    [InlineData("ABC1D23")]
    [InlineData("bra2e19")]
    public void Aceita_placa_antiga_e_mercosul(string placa) => Assert.True(PlacaVeiculo.EhValida(placa));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ABC123")]
    [InlineData("AB12345")]
    [InlineData("ABCD123")]
    [InlineData("ABC12D3")]
    [InlineData("ABC1D234")]
    public void Rejeita_placa_invalida(string? placa) => Assert.False(PlacaVeiculo.EhValida(placa));
}
