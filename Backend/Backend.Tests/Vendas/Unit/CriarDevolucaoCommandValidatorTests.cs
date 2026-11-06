using Backend.Core.Features.Vendas.Commands;
using Backend.Core.Features.Vendas.Validators.Commands;

namespace Backend.Tests.Vendas.Unit;

public class CriarDevolucaoCommandValidatorTests
{
    private static bool Valido(string motivo)
        => new CriarDevolucaoCommandValidator()
            .Validate(new CriarDevolucaoCommand(motivo, [new CriarDevolucaoItemCommand(1, 1)]))
            .Errors.All(e => e.PropertyName != nameof(CriarDevolucaoCommand.Motivo));

    [Fact] public void Motivo_de_4_caracteres_e_invalido() => Assert.False(Valido(new string('a', 4)));
    [Fact] public void Motivo_de_5_caracteres_e_valido() => Assert.True(Valido(new string('a', 5)));
    [Fact] public void Motivo_de_255_caracteres_e_valido() => Assert.True(Valido(new string('a', 255)));
    [Fact] public void Motivo_de_256_caracteres_e_invalido() => Assert.False(Valido(new string('a', 256)));
}
