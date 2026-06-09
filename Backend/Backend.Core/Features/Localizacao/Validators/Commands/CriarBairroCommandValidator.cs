using Backend.Core.Features.Localizacao.Commands;
using FluentValidation;

namespace Backend.Core.Features.Localizacao.Validators.Commands;

public class CriarBairroCommandValidator : AbstractValidator<CriarBairroCommand>
{
    public CriarBairroCommandValidator()
    {
        RuleFor(x => x.Bairro).NotEmpty().MaximumLength(100);
        RuleFor(x => x.CidadeId).GreaterThan(0);
    }
}
