using Backend.Core.Common.ValueObjects;
using Backend.Core.Features.Localizacao.DTOs;
using FluentValidation;

namespace Backend.Core.Features.Localizacao.Validators;

public sealed class UpdateCidadeDtoValidator : AbstractValidator<UpdateCidadeDto>
{
  public UpdateCidadeDtoValidator()
  {
    RuleFor(x => x.Cidade)
        .NotEmpty().WithMessage("Cidade é obrigatória.")
        .WithErrorCode("CIDADE_OBRIGATORIA");

    RuleFor(x => x.Ddd)
        .GreaterThan((short)0).WithMessage("DDD deve ser maior que zero.")
        .WithErrorCode("DDD_INVALIDO");

    RuleFor(x => x.EstadoId)
        .GreaterThan(0).WithMessage("Estado é obrigatório.")
        .WithErrorCode("ESTADO_OBRIGATORIO");
  }
}
