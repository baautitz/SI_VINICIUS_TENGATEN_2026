using Backend.Core.Features.Localizacao.DTOs;
using FluentValidation;

namespace Backend.Core.Features.Localizacao.Validators;

public sealed class CreateEstadoDtoValidator : AbstractValidator<CreateEstadoDto>
{
  public CreateEstadoDtoValidator()
  {
    RuleFor(x => x.Estado)
        .NotEmpty().WithMessage("Estado é obrigatório.")
        .WithErrorCode("ESTADO_OBRIGATORIO");

    RuleFor(x => x.Uf)
        .NotEmpty().WithMessage("UF é obrigatório.")
        .WithErrorCode("UF_OBRIGATORIO")
        .Length(2).WithMessage("UF deve ter 2 caracteres.")
        .WithErrorCode("UF_TAMANHO_INVALIDO")
        .Matches("^[A-Z]+$").WithMessage("UF deve conter apenas letras maiúsculas.")
        .WithErrorCode("UF_FORMATO_INVALIDO");

    RuleFor(x => x.PaisId)
        .GreaterThan(0).WithMessage("País é obrigatório.")
        .WithErrorCode("PAIS_OBRIGATORIO");
  }
}
