using Backend.Core.Features.Catalogo.Commands;
using FluentValidation;

namespace Backend.Core.Features.Catalogo.Validators.Commands;

public class CriarUnidadeMedidaCommandValidator : AbstractValidator<CriarUnidadeMedidaCommand>
{
    public CriarUnidadeMedidaCommandValidator()
    {
        RuleFor(x => x.Sigla).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Descricao).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Categoria).NotEmpty().MaximumLength(50);
    }
}
