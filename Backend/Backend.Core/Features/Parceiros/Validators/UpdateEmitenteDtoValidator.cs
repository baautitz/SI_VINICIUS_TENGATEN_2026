using Backend.Core.Common.ValueObjects;
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

        RuleFor(x => x.TipoPessoa)
            .IsInEnum().WithMessage("Tipo de Pessoa inválido.");

        RuleFor(x => x.NacionalidadeId)
            .GreaterThan(0).WithMessage("Nacionalidade é obrigatória.");
    }
}
