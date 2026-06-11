using Backend.Core.Features.Localizacao.Commands;
using FluentValidation;

namespace Backend.Core.Features.Localizacao.Validators.Commands;

public class CriarCidadeCommandValidator : AbstractValidator<CriarCidadeCommand>
{
    public CriarCidadeCommandValidator()
    {
        RuleFor(x => x.Cidade).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Ddd).NotEmpty().Matches(@"^\d{2,4}$").WithMessage("DDD deve conter entre 2 e 4 números.");
        RuleFor(x => x.EstadoId).GreaterThan(0);
    }
}
