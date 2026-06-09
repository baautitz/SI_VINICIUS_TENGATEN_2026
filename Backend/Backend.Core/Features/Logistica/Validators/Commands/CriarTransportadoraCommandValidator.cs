using Backend.Core.Common.ValueObjects;
using Backend.Core.Features.Logistica.Commands;
using FluentValidation;

namespace Backend.Core.Features.Logistica.Validators.Commands;

public class CriarTransportadoraCommandValidator : AbstractValidator<CriarTransportadoraCommand>
{
    public CriarTransportadoraCommandValidator()
    {
        RuleFor(x => x.NomeRazaosocial)
            .NotEmpty().WithMessage("Nome ou Razão Social é obrigatório.")
            .MaximumLength(150).WithMessage("Nome ou Razão Social deve ter no máximo 150 caracteres.");

        RuleFor(x => x.CpfCnpj)
            .NotEmpty().WithMessage("CPF ou CNPJ é obrigatório.")
            .Must(BeValidDocument).WithMessage("CPF ou CNPJ inválido.");

        RuleFor(x => x.NacionalidadeId)
            .GreaterThan(0).WithMessage("Nacionalidade é obrigatória.");

        RuleFor(x => x.RgIe)
            .MaximumLength(50).WithMessage("RG ou Inscrição Estadual deve ter no máximo 50 caracteres.");

        RuleFor(x => x.ApelidoNomefantasia)
            .MaximumLength(150).WithMessage("Apelido ou Nome Fantasia deve ter no máximo 150 caracteres.");

        RuleFor(x => x.Endereco)
            .MaximumLength(255).WithMessage("Endereço deve ter no máximo 255 caracteres.");

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
