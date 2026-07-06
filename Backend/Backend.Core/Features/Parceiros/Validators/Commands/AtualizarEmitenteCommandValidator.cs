using Backend.Core.Features.Parceiros.Commands;
using Backend.Core.Features.Parceiros.Enums;
using FluentValidation;

namespace Backend.Core.Features.Parceiros.Validators.Commands;

public class AtualizarEmitenteCommandValidator : AbstractValidator<AtualizarEmitenteCommand>
{
    public AtualizarEmitenteCommandValidator()
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

        RuleFor(x => x.Sexo)
            .Must(s => string.IsNullOrEmpty(s) || s == "M" || s == "F" || s == "O")
            .WithMessage("Sexo inválido. Escolha entre M, F ou O.");

        RuleFor(x => x.Sexo)
            .Empty().When(x => x.TipoPessoa == TipoPessoa.JURIDICA)
            .WithMessage("Sexo só pode ser informado para pessoa física.");
    }
}
