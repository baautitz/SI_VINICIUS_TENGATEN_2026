using Backend.Core.Features.Localizacao.DTOs;
using FluentValidation;

namespace Backend.Core.Features.Localizacao.Validators;

public sealed class CreateEstadoDtoValidator : AbstractValidator<CreateEstadoDto>
{
  public CreateEstadoDtoValidator()
  {
    RuleFor(x => x.Estado)
        .NotEmpty().WithMessage("Estado é obrigatório.");

    RuleFor(x => x.Uf)
        .NotEmpty().WithMessage("UF é obrigatório.")
        .Length(2).WithMessage("UF deve ter 2 caracteres.")
        .Matches("^[A-Z]+$").WithMessage("UF deve conter apenas letras maiúsculas.");

    RuleFor(x => x.PaisId)
        .GreaterThan(0).WithMessage("País é obrigatório.");
  }
}
