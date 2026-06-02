using Backend.Core.Features.Logistica.DTOs;
using FluentValidation;

namespace Backend.Core.Features.Logistica.Validators;

public class CreateTransportadoraDtoValidator : AbstractValidator<CreateTransportadoraDto>
{
    public CreateTransportadoraDtoValidator()
    {
        RuleFor(x => x.NomeRazaosocial)
            .NotEmpty().WithMessage("Nome ou Razão Social é obrigatório.")
            .MaximumLength(150).WithMessage("Nome ou Razão Social deve ter no máximo 150 caracteres.");

        RuleFor(x => x.CpfCnpj)
            .NotEmpty().WithMessage("CPF/CNPJ é obrigatório.")
            .MaximumLength(20).WithMessage("CPF/CNPJ deve ter no máximo 20 caracteres.");

        RuleFor(x => x.Rntrc)
            .MaximumLength(20).WithMessage("RNTRC deve ter no máximo 20 caracteres.");
    }
}
