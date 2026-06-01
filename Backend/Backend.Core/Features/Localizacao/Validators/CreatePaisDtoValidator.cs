using Backend.Core.Features.Localizacao.DTOs;
using FluentValidation;

namespace Backend.Core.Features.Localizacao.Validators;

public sealed class CreatePaisDtoValidator : AbstractValidator<CreatePaisDto>
{
  public CreatePaisDtoValidator()
  {
    RuleFor(x => x.Ddi)
        .NotEmpty().WithMessage("DDI é obrigatório.")
        .WithErrorCode("DDI_OBRIGATORIO")
        .Matches("^\\+\\d+$").WithMessage("DDI deve ser no formato +55, +1, etc.")
        .WithErrorCode("DDI_FORMATO_INVALIDO")
        .MaximumLength(5).WithMessage("DDI deve ter no máximo 5 caracteres.")
        .WithErrorCode("DDI_TAMANHO_INVALIDO");

    RuleFor(x => x.SiglaIso)
        .NotEmpty().WithMessage("Sigla ISO é obrigatória.")
        .WithErrorCode("SIGLAISO_OBRIGATORIO")
        .Length(3).WithMessage("Sigla ISO deve ter 3 caracteres.")
        .WithErrorCode("SIGLAISO_TAMANHO_INVALIDO")
        .Matches("^[A-Z]+$").WithMessage("Sigla ISO deve conter apenas letras maiúsculas.")
        .WithErrorCode("SIGLAISO_FORMATO_INVALIDO");

    RuleFor(x => x.Moeda)
        .NotEmpty().WithMessage("Moeda é obrigatória.")
        .WithErrorCode("MOEDA_OBRIGATORIA")
        .Length(3).WithMessage("Moeda deve ter 3 caracteres.")
        .WithErrorCode("MOEDA_TAMANHO_INVALIDO");

    RuleFor(x => x.SimboloMoeda)
        .NotEmpty().WithMessage("Símbolo da moeda é obrigatório.")
        .WithErrorCode("SIMBOLO_MOEDA_OBRIGATORIO")
        .MaximumLength(5).WithMessage("Símbolo da moeda deve ter no máximo 5 caracteres.")
        .WithErrorCode("SIMBOLO_MOEDA_TAMANHO_INVALIDO");

    RuleFor(x => x.Pais)
        .NotEmpty().WithMessage("País é obrigatório.")
        .WithErrorCode("PAIS_OBRIGATORIO")
        .MaximumLength(60).WithMessage("País deve ter no máximo 60 caracteres.")
        .WithErrorCode("PAIS_TAMANHO_INVALIDO");
  }
}
