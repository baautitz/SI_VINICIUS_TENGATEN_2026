using Backend.Core.Features.Localizacao.DTOs;
using FluentValidation;

namespace Backend.Core.Features.Localizacao.Validators;

public sealed class UpdatePaisDtoValidator : AbstractValidator<UpdatePaisDto>
{
  public UpdatePaisDtoValidator()
  {
    RuleFor(x => x.Ddi)
        .NotEmpty().WithMessage("DDI é obrigatório.")
        .Matches("^\\+\\d+$").WithMessage("DDI deve ser no formato +55, +1, etc.");

    RuleFor(x => x.SiglaIso)
        .NotEmpty().WithMessage("Sigla ISO é obrigatória.")
        .Length(2).WithMessage("Sigla ISO deve ter 2 caracteres.")
        .Matches("^[A-Z]+$").WithMessage("Sigla ISO deve conter apenas letras maiúsculas.");

    RuleFor(x => x.Moeda)
        .NotEmpty().WithMessage("Moeda é obrigatória.");

    RuleFor(x => x.SimboloMoeda)
        .NotEmpty().WithMessage("Símbolo da moeda é obrigatório.");

    RuleFor(x => x.Pais)
        .NotEmpty().WithMessage("País é obrigatório.");
  }
}
