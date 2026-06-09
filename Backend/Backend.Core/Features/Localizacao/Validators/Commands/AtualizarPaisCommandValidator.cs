namespace Backend.Core.Features.Localizacao.Validators.Commands;

using Backend.Core.Features.Localizacao.Commands;
using FluentValidation;

public class AtualizarPaisCommandValidator : AbstractValidator<AtualizarPaisCommand>
{
    public AtualizarPaisCommandValidator()
    {
        RuleFor(x => x.Pais).NotEmpty().MaximumLength(100);
        RuleFor(x => x.SiglaIso).NotEmpty().Length(3);
        RuleFor(x => x.Ddi).NotEmpty().MaximumLength(5);
        RuleFor(x => x.Moeda).NotEmpty().MaximumLength(50);
        RuleFor(x => x.SimboloMoeda).NotEmpty().MaximumLength(5);
    }
}
