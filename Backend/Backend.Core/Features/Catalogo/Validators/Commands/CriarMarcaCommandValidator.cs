using Backend.Core.Features.Catalogo.Commands;
using FluentValidation;

namespace Backend.Core.Features.Catalogo.Validators.Commands;

public class CriarMarcaCommandValidator : AbstractValidator<CriarMarcaCommand>
{
    public CriarMarcaCommandValidator()
    {
        RuleFor(x => x.Marca)
            .NotEmpty().WithMessage("O nome da marca é obrigatório.")
            .MaximumLength(100).WithMessage("O nome da marca deve ter no máximo 100 caracteres.");
    }
}
