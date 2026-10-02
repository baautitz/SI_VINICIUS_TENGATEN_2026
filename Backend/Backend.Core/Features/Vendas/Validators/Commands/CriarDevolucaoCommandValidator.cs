using Backend.Core.Features.Vendas.Commands;
using FluentValidation;

namespace Backend.Core.Features.Vendas.Validators.Commands;

public class CriarDevolucaoCommandValidator : AbstractValidator<CriarDevolucaoCommand>
{
    public CriarDevolucaoCommandValidator()
    {
        RuleFor(x => x.Motivo)
            .NotEmpty().WithMessage("Motivo da devolução é obrigatório.")
            .MinimumLength(5).WithMessage("Motivo da devolução deve ter pelo menos 5 caracteres.")
            .MaximumLength(500).WithMessage("Motivo da devolução não pode ter mais de 500 caracteres.");

        RuleFor(x => x.Itens)
            .NotEmpty().WithMessage("Informe ao menos um item para devolver.");

        RuleForEach(x => x.Itens)
            .Must(i => i.Quantidade >= 0).WithMessage("Quantidade devolvida não pode ser negativa.");
    }
}
