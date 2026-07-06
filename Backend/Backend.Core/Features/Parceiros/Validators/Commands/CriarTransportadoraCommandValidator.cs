using Backend.Core.Common.ValueObjects;
using Backend.Core.Features.Parceiros.Commands;
using Backend.Core.Features.Parceiros.Enums;
using FluentValidation;

namespace Backend.Core.Features.Parceiros.Validators.Commands;

public class CriarTransportadoraCommandValidator : AbstractValidator<CriarTransportadoraCommand>
{
    public CriarTransportadoraCommandValidator()
    {
        RuleFor(x => x.NomeRazaosocial)
            .NotEmpty().WithMessage("Nome ou Razão Social é obrigatório.")
            .MaximumLength(150).WithMessage("Nome ou Razão Social deve ter no máximo 150 caracteres.");

        RuleFor(x => x.CpfCnpj)
            .NotEmpty().WithMessage("CPF/CNPJ ou Documento é obrigatório.")
            .Must(BeValidDocument).WithMessage("CPF/CNPJ ou Documento inválido.");

        RuleFor(x => x.NacionalidadeId)
            .GreaterThan(0).WithMessage("Nacionalidade é obrigatória.");

        RuleFor(x => x.RgIe)
            .MaximumLength(50).WithMessage("RG ou Inscrição Estadual deve ter no máximo 50 caracteres.");

        RuleFor(x => x.ApelidoNomefantasia)
            .MaximumLength(150).WithMessage("Apelido ou Nome Fantasia deve ter no máximo 150 caracteres.");

        RuleFor(x => x.Logradouro)
            .MaximumLength(150).WithMessage("Logradouro deve ter no máximo 150 caracteres.");

        RuleFor(x => x.Numero)
            .MaximumLength(20).WithMessage("Número deve ter no máximo 20 caracteres.");

        RuleFor(x => x.Telefone)
            .MaximumLength(20).WithMessage("Telefone deve ter no máximo 20 caracteres.");

        RuleFor(x => x.Email)
            .MaximumLength(150).WithMessage("E-mail deve ter no máximo 150 caracteres.")
            .EmailAddress().WithMessage("E-mail inválido.")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.Rntrc)
            .MaximumLength(20).WithMessage("RNTRC deve ter no máximo 20 caracteres.");

        RuleFor(x => x.Observacao)
            .MaximumLength(500).WithMessage("Observação deve ter no máximo 500 caracteres.");

        RuleFor(x => x.Sexo)
            .Must(s => string.IsNullOrEmpty(s) || s == "M" || s == "F" || s == "O")
            .WithMessage("Sexo inválido. Escolha entre M, F ou O.");

        RuleFor(x => x.Sexo)
            .Empty().When(x => x.TipoPessoa == TipoPessoa.JURIDICA)
            .WithMessage("Sexo só pode ser informado para pessoa física.");
    }

    private bool BeValidDocument(string doc)
    {
        try
        {
            var _ = new DocumentoGenerico(doc);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
