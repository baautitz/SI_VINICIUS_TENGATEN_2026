using Backend.Core.Features.Vendas.Commands;
using FluentValidation;

namespace Backend.Core.Features.Vendas.Validators.Commands;

public class CancelarVendaCommandValidator : AbstractValidator<CancelarVendaCommand>
{
    public CancelarVendaCommandValidator()
    {
        RuleFor(x => x.Motivo)
            .NotEmpty().WithMessage("Motivo do cancelamento é obrigatório.")
            .MinimumLength(5).WithMessage("Motivo do cancelamento deve ter pelo menos 5 caracteres.")
            .MaximumLength(500).WithMessage("Motivo do cancelamento não pode ter mais de 500 caracteres.");
    }
}
