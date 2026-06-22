using Backend.Core.Features.Financeiro.Commands;
using FluentValidation;

namespace Backend.Core.Features.Financeiro.Validators.Commands;

public class CriarMetodoPagamentoCommandValidator : AbstractValidator<CriarMetodoPagamentoCommand>
{
    public CriarMetodoPagamentoCommandValidator()
    {
        RuleFor(x => x.Codigo)
            .MaximumLength(10).WithMessage("Código do método de pagamento deve ter no máximo 10 caracteres.")
            .When(x => !string.IsNullOrEmpty(x.Codigo));

        RuleFor(x => x.Descricao)
            .NotEmpty().WithMessage("Descrição do método de pagamento é obrigatória.")
            .MaximumLength(100).WithMessage("Descrição do método de pagamento deve ter no máximo 100 caracteres.");
    }
}
