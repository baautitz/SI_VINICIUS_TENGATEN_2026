using Backend.Core.Features.Localizacao.DTOs;
using FluentValidation;

namespace Backend.Core.Features.Localizacao.Validators;

public sealed class CreateCidadeDtoValidator : AbstractValidator<CreateCidadeDto>
{
  public CreateCidadeDtoValidator()
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
