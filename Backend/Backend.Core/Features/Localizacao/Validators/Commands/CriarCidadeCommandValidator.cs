using Backend.Core.Features.Localizacao.Commands;
using FluentValidation;

namespace Backend.Core.Features.Localizacao.Validators.Commands;

public class CriarCidadeCommandValidator : AbstractValidator<CriarCidadeCommand>
{
    public CriarCidadeCommandValidator()
    {
        RuleFor(x => x.Cidade).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Ddd).NotEmpty().Length(2);
        RuleFor(x => x.EstadoId).GreaterThan(0);
    }
}
