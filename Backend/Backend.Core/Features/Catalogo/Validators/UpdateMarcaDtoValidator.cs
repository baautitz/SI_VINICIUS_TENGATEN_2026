using Backend.Core.Features.Catalogo.DTOs;
using FluentValidation;

namespace Backend.Core.Features.Catalogo.Validators;

public sealed class UpdateMarcaDtoValidator : AbstractValidator<UpdateMarcaDto>
{
    public UpdateMarcaDtoValidator()
    {
        RuleFor(x => x.Marca)
            .NotEmpty().WithMessage("Marca é obrigatória.")
            .WithErrorCode("MARCA_OBRIGATORIO")
            .MaximumLength(100).WithMessage("Marca deve ter no máximo 100 caracteres.")
            .WithErrorCode("MARCA_TAMANHO_INVALIDO");

        RuleFor(x => x.Descricao)
            .MaximumLength(255).WithMessage("Descrição deve ter no máximo 255 caracteres.")
            .WithErrorCode("DESCRICAO_TAMANHO_INVALIDO");
    }
}
