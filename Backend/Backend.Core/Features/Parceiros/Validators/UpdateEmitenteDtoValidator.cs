using Backend.Core.Features.Parceiros.DTOs;
using FluentValidation;

namespace Backend.Core.Features.Parceiros.Validators;

public class UpdateEmitenteDtoValidator : AbstractValidator<UpdateEmitenteDto>
{
    public UpdateEmitenteDtoValidator()
    {
        RuleFor(x => x.NomeRazaoSocial)
            .NotEmpty().WithMessage("Nome ou Razão Social é obrigatório.")
            .MaximumLength(255).WithMessage("Nome ou Razão Social deve ter no máximo 255 caracteres.");

        RuleFor(x => x.CpfCnpj)
            .NotEmpty().WithMessage("CPF/CNPJ ou Documento é obrigatório.")
            .MaximumLength(20).WithMessage("CPF/CNPJ ou Documento deve ter no máximo 20 caracteres.");
    }
}
