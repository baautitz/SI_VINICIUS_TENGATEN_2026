using Backend.Core.Features.Localizacao.DTOs;
using FluentValidation;

namespace Backend.Core.Features.Localizacao.Validators;

public sealed class UpdatePaisDtoValidator : AbstractValidator<UpdatePaisDto>
{
  public UpdatePaisDtoValidator()
  {
    RuleFor(x => x.Ddi)
        .NotEmpty().WithMessage("DDI é obrigatório.")
        .WithErrorCode("DDI_OBRIGATORIO")
        .Matches("^\\+\\d+$").WithMessage("DDI deve ser no formato +55, +1, etc.")
        .WithErrorCode("DDI_FORMATO_INVALIDO");

    RuleFor(x => x.SiglaIso)
        .NotEmpty().WithMessage("Sigla ISO é obrigatória.")
        .WithErrorCode("SIGLAISO_OBRIGATORIO")
        .Length(2).WithMessage("Sigla ISO deve ter 2 caracteres.")
        .WithErrorCode("SIGLAISO_TAMANHO_INVALIDO")
        .Matches("^[A-Z]+$").WithMessage("Sigla ISO deve conter apenas letras maiúsculas.")
        .WithErrorCode("SIGLAISO_FORMATO_INVALIDO");

    RuleFor(x => x.Moeda)
        .NotEmpty().WithMessage("Moeda é obrigatória.")
        .WithErrorCode("MOEDA_OBRIGATORIA");

    RuleFor(x => x.SimboloMoeda)
        .NotEmpty().WithMessage("Símbolo da moeda é obrigatório.")
        .WithErrorCode("SIMBOLO_MOEDA_OBRIGATORIO");

    RuleFor(x => x.Pais)
        .NotEmpty().WithMessage("País é obrigatório.")
        .WithErrorCode("PAIS_OBRIGATORIO");
  }
}
