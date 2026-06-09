using Backend.Core.Features.Localizacao.Commands;
using FluentValidation;

namespace Backend.Core.Features.Localizacao.Validators.Commands;

public class AtualizarBairroCommandValidator : AbstractValidator<AtualizarBairroCommand>
{
    public AtualizarBairroCommandValidator()
    {
        RuleFor(x => x.Bairro).NotEmpty().MaximumLength(100);
        RuleFor(x => x.CidadeId).GreaterThan(0);
    }
}
