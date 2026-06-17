using Backend.Core.Features.Financeiro.Commands;
using FluentValidation;
using System.Linq;

namespace Backend.Core.Features.Financeiro.Validators.Commands;

public class CriarContaReceberCommandValidator : AbstractValidator<CriarContaReceberCommand>
{
    public CriarContaReceberCommandValidator()
    {
        RuleFor(x => x.Descricao)
            .NotEmpty().WithMessage("Descrição da conta a receber é obrigatória.")
            .MaximumLength(150).WithMessage("Descrição deve ter no máximo 150 caracteres.");

        RuleFor(x => x.ClienteId)
            .GreaterThan(0).WithMessage("Cliente é obrigatório.");

        RuleFor(x => x.ValorOriginal)
            .GreaterThan(0).WithMessage("Valor original deve ser maior que zero.");

        RuleFor(x => x.Parcelas)
            .NotEmpty().WithMessage("A conta a receber deve conter ao menos uma parcela.");

        RuleForEach(x => x.Parcelas).ChildRules(parcela =>
        {
            parcela.RuleFor(p => p.NumeroParcela)
                .GreaterThan(0).WithMessage("Número da parcela deve ser maior que zero.");

            parcela.RuleFor(p => p.DataVencimento)
                .NotEmpty().WithMessage("Data de vencimento da parcela é obrigatória.");

            parcela.RuleFor(p => p.ValorParcela)
                .GreaterThan(0).WithMessage("Valor da parcela deve ser maior que zero.");
        });

        RuleFor(x => x)
            .Must(x => x.Parcelas != null && x.Parcelas.Sum(p => p.ValorParcela) == x.ValorOriginal)
            .WithMessage("A soma dos valores das parcelas deve ser exatamente igual ao valor original da conta.")
            .When(x => x.ValorOriginal > 0 && x.Parcelas != null);
    }
}
