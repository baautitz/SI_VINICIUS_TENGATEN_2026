using Backend.Core.Features.Catalogo.DTOs;
using FluentValidation;

namespace Backend.Core.Features.Catalogo.Validators;

public sealed class CreateCategoriaDtoValidator : AbstractValidator<CreateCategoriaDto>
{
    public CreateCategoriaDtoValidator()
    {
        RuleFor(x => x.Categoria)
            .NotEmpty().WithMessage("Categoria é obrigatória.")
            .WithErrorCode("CATEGORIA_OBRIGATORIO")
            .MaximumLength(100).WithMessage("Categoria deve ter no máximo 100 caracteres.")
            .WithErrorCode("CATEGORIA_TAMANHO_INVALIDO");

        RuleFor(x => x.Descricao)
            .MaximumLength(255).WithMessage("Descrição deve ter no máximo 255 caracteres.")
            .WithErrorCode("DESCRICAO_TAMANHO_INVALIDO");
    }
}
