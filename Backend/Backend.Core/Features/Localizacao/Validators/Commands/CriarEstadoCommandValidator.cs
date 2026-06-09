using Backend.Core.Features.Localizacao.Commands;
using FluentValidation;

namespace Backend.Core.Features.Localizacao.Validators.Commands;

public class CriarEstadoCommandValidator : AbstractValidator<CriarEstadoCommand>
{
    public CriarEstadoCommandValidator()
    {
        RuleFor(x => x.Estado).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Uf).NotEmpty().Length(2);
        RuleFor(x => x.PaisId).GreaterThan(0);
    }
}
