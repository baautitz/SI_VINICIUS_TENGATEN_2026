using Backend.Core.Features.Financeiro.DTOs;
using FluentValidation;

namespace Backend.Core.Features.Financeiro.Validators;

// Valida as faixas dos filtros de contas a pagar e a receber (mínimo > máximo, "de" depois de "até").
public class FiltroTitulosValidator : AbstractValidator<IFiltroTitulos>
{
    private const string Codigo = "VALIDATION_ERROR";

    public FiltroTitulosValidator()
    {
        RuleFor(x => x.ValorMinimo)
            .LessThanOrEqualTo(x => x.ValorMaximo!.Value)
            .When(x => x.ValorMinimo.HasValue && x.ValorMaximo.HasValue)
            .WithErrorCode(Codigo).WithMessage("O valor mínimo não pode ser maior que o valor máximo.");

        RuleFor(x => x.VencimentoDe)
            .LessThanOrEqualTo(x => x.VencimentoAte!.Value)
            .When(x => x.VencimentoDe.HasValue && x.VencimentoAte.HasValue)
            .WithErrorCode(Codigo).WithMessage("O vencimento inicial não pode ser posterior ao vencimento final.");

        RuleFor(x => x.EmissaoDe)
            .LessThanOrEqualTo(x => x.EmissaoAte!.Value)
            .When(x => x.EmissaoDe.HasValue && x.EmissaoAte.HasValue)
            .WithErrorCode(Codigo).WithMessage("A emissão inicial não pode ser posterior à emissão final.");
    }
}
