using Backend.Core.Features.Catalogo.DTOs;
using FluentValidation;

namespace Backend.Core.Features.Catalogo.Validators;

public sealed class UpdateSkuAtributosChavesDtoValidator : AbstractValidator<UpdateSkuAtributosChavesDto>
{
    public UpdateSkuAtributosChavesDtoValidator()
    {
        RuleFor(x => x.Chave)
            .NotEmpty().WithMessage("Chave de atributo é obrigatória.")
            .WithErrorCode("CHAVE_OBRIGATORIO")
            .MaximumLength(100).WithMessage("Chave de atributo deve ter no máximo 100 caracteres.")
            .WithErrorCode("CHAVE_TAMANHO_INVALIDO");

        RuleFor(x => x.Valores)
            .NotEmpty().WithMessage("Atributo deve ter pelo menos um valor cadastrado.")
            .WithErrorCode("VALORES_OBRIGATORIOS");

        RuleForEach(x => x.Valores)
            .NotEmpty().WithMessage("Valor do atributo não pode ser vazio.")
            .WithErrorCode("VALOR_ATRIBUTO_OBRIGATORIO")
            .MaximumLength(150).WithMessage("Cada valor de atributo deve ter no máximo 150 caracteres.")
            .WithErrorCode("VALOR_ATRIBUTO_TAMANHO_INVALIDO");
    }
}
