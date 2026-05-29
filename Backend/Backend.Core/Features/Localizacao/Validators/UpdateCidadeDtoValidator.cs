using Backend.Core.Features.Localizacao.DTOs;
using FluentValidation;

namespace Backend.Core.Features.Localizacao.Validators;

public sealed class UpdateCidadeDtoValidator : AbstractValidator<UpdateCidadeDto>
{
  public UpdateCidadeDtoValidator()
  {
    RuleFor(x => x.Cidade)
        .NotEmpty().WithMessage("Cidade é obrigatória.");

    RuleFor(x => x.Ddd)
        .GreaterThan((short)0).WithMessage("DDD deve ser maior que zero.");

    RuleFor(x => x.EstadoId)
        .GreaterThan(0).WithMessage("Estado é obrigatório.");
  }
}
