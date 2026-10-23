using Backend.Core.Features.Financeiro.Commands;
using FluentValidation;

namespace Backend.Core.Features.Financeiro.Validators.Commands;

public class CriarContaPagarCommandValidator : AbstractValidator<CriarContaPagarCommand>
{
    public CriarContaPagarCommandValidator()
    {
        RuleFor(x => x.Descricao)
            .NotEmpty().WithMessage("Descrição da conta a pagar é obrigatória.")
            .MaximumLength(150).WithMessage("Descrição deve ter no máximo 150 caracteres.");

        RuleFor(x => x.FornecedorId)
            .GreaterThan(0).WithMessage("Fornecedor é obrigatório.");

        RuleFor(x => x.NumeroDocumento)
            .MaximumLength(30).WithMessage("Número do documento deve ter no máximo 30 caracteres.");

        RuleFor(x => x.Parcelas)
            .NotEmpty().WithMessage("A conta a pagar deve conter ao menos uma parcela.");

        RuleForEach(x => x.Parcelas).ChildRules(parcela =>
        {
            parcela.RuleFor(p => p.DataVencimento)
                .NotEmpty().WithMessage("Data de vencimento da parcela é obrigatória.");

            parcela.RuleFor(p => p.Valor)
                .GreaterThan(0).WithMessage("Valor da parcela deve ser maior que zero.");
        });
    }
}
