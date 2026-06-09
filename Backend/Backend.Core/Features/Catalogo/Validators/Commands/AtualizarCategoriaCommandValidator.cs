using Backend.Core.Features.Catalogo.Commands;
using FluentValidation;

namespace Backend.Core.Features.Catalogo.Validators.Commands;

public class AtualizarCategoriaCommandValidator : AbstractValidator<AtualizarCategoriaCommand>
{
    public AtualizarCategoriaCommandValidator()
    {
        RuleFor(x => x.Categoria)
            .NotEmpty().WithMessage("O nome da categoria é obrigatório.")
            .MaximumLength(100).WithMessage("O nome da categoria deve ter no máximo 100 caracteres.");
    }
}
