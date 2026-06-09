using Backend.Core.Features.Catalogo.Commands;
using FluentValidation;

namespace Backend.Core.Features.Catalogo.Validators.Commands;

public class AtualizarSkuAtributosChavesCommandValidator : AbstractValidator<AtualizarSkuAtributosChavesCommand>
{
    public AtualizarSkuAtributosChavesCommandValidator()
    {
        RuleFor(x => x.Chave).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Valores).NotEmpty();
    }
}
