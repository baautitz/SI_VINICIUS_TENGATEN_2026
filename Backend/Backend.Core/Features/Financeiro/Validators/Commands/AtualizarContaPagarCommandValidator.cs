using Backend.Core.Features.Financeiro.Commands;
using FluentValidation;

namespace Backend.Core.Features.Financeiro.Validators.Commands;

public class AtualizarContaPagarCommandValidator : AbstractValidator<AtualizarContaPagarCommand>
{
    public AtualizarContaPagarCommandValidator()
    {
        RuleFor(x => x.Descricao)
            .NotEmpty().WithMessage("Descrição da conta a pagar é obrigatória.")
            .MaximumLength(150).WithMessage("Descrição deve ter no máximo 150 caracteres.");

        RuleFor(x => x.DataVencimento)
            .NotEmpty().WithMessage("Data de vencimento é obrigatória.");

        RuleFor(x => x.Valor)
            .GreaterThan(0).WithMessage("Valor deve ser maior que zero.");
    }
}
