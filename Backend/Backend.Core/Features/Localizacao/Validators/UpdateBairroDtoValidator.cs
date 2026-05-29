using Backend.Core.Features.Localizacao.DTOs;
using FluentValidation;

namespace Backend.Core.Features.Localizacao.Validators;

public sealed class UpdateBairroDtoValidator : AbstractValidator<UpdateBairroDto>
{
  public UpdateBairroDtoValidator()
  {
    RuleFor(x => x.Bairro)
        .NotEmpty().WithMessage("Bairro é obrigatório.");

    RuleFor(x => x.CidadeId)
        .GreaterThan(0).WithMessage("Cidade é obrigatória.");
  }
}
