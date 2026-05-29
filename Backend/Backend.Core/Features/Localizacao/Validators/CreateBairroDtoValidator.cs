using Backend.Core.Features.Localizacao.DTOs;
using FluentValidation;

namespace Backend.Core.Features.Localizacao.Validators;

public sealed class CreateBairroDtoValidator : AbstractValidator<CreateBairroDto>
{
  public CreateBairroDtoValidator()
  {
    RuleFor(x => x.Bairro)
        .NotEmpty().WithMessage("Bairro é obrigatório.")
        .WithErrorCode("BAIRRO_OBRIGATORIO");

    RuleFor(x => x.CidadeId)
        .GreaterThan(0).WithMessage("Cidade é obrigatória.")
        .WithErrorCode("CIDADE_OBRIGATORIA");
  }
}
