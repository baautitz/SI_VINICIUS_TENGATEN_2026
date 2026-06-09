using Backend.Core.Features.Localizacao.Commands;
using FluentValidation;

namespace Backend.Core.Features.Localizacao.Validators.Commands;

public class AtualizarCidadeCommandValidator : AbstractValidator<AtualizarCidadeCommand>
{
    public AtualizarCidadeCommandValidator()
    {
        RuleFor(x => x.Cidade).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Ddd).NotEmpty().Length(2);
        RuleFor(x => x.EstadoId).GreaterThan(0);
    }
}
