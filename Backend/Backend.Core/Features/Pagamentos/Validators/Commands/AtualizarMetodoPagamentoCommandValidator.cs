using Backend.Core.Features.Pagamentos.Commands;
using FluentValidation;

namespace Backend.Core.Features.Pagamentos.Validators.Commands;

public class ResumoAtualizarMetodoPagamentoCommandValidator : AbstractValidator<AtualizarMetodoPagamentoCommand>
{
    public ResumoAtualizarMetodoPagamentoCommandValidator()
    {
        RuleFor(x => x.Descricao)
            .NotEmpty().WithMessage("Descrição do método de pagamento é obrigatória.")
            .MaximumLength(100).WithMessage("Descrição do método de pagamento deve ter no máximo 100 caracteres.");
    }
}
