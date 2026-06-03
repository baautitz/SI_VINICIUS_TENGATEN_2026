using Backend.Core.Features.Catalogo.DTOs;
using FluentValidation;

namespace Backend.Core.Features.Catalogo.Validators;

public sealed class CreateUnidadeMedidaDtoValidator : AbstractValidator<CreateUnidadeMedidaDto>
{
    public CreateUnidadeMedidaDtoValidator()
    {
        RuleFor(x => x.Sigla)
            .NotEmpty().WithMessage("Sigla é obrigatória.")
            .WithErrorCode("SIGLA_OBRIGATORIO")
            .MaximumLength(10).WithMessage("Sigla deve ter no máximo 10 caracteres.")
            .WithErrorCode("SIGLA_TAMANHO_INVALIDO");

        RuleFor(x => x.Descricao)
            .NotEmpty().WithMessage("Descrição é obrigatória.")
            .WithErrorCode("DESCRICAO_OBRIGATORIO")
            .MaximumLength(100).WithMessage("Descrição deve ter no máximo 100 caracteres.")
            .WithErrorCode("DESCRICAO_TAMANHO_INVALIDO");

        RuleFor(x => x.Categoria)
            .NotEmpty().WithMessage("Categoria é obrigatória.")
            .WithErrorCode("CATEGORIA_OBRIGATORIA")
            .MaximumLength(50).WithMessage("Categoria deve ter no máximo 50 caracteres.")
            .WithErrorCode("CATEGORIA_TAMANHO_INVALIDO");
    }
}
