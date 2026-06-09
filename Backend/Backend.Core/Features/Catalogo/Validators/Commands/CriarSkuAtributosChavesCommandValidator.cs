using Backend.Core.Features.Catalogo.Commands;
using FluentValidation;

namespace Backend.Core.Features.Catalogo.Validators.Commands;

public class CriarSkuAtributosChavesCommandValidator : AbstractValidator<CriarSkuAtributosChavesCommand>
{
    public CriarSkuAtributosChavesCommandValidator()
    {
        RuleFor(x => x.Chave).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Valores).NotEmpty();
    }
}
