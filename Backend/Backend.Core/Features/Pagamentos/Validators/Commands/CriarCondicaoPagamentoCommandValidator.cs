using Backend.Core.Features.Pagamentos.Commands;
using FluentValidation;
using System.Linq;

namespace Backend.Core.Features.Pagamentos.Validators.Commands;

public class CriarCondicaoPagamentoCommandValidator : AbstractValidator<CriarCondicaoPagamentoCommand>
{
    public CriarCondicaoPagamentoCommandValidator()
    {
        RuleFor(x => x.Descricao)
            .NotEmpty().WithMessage("Descrição da condição de pagamento é obrigatória.")
            .MaximumLength(150).WithMessage("Descrição da condição de pagamento deve ter no máximo 150 caracteres.");

        RuleFor(x => x.MetodoPagamentoCodigo)
            .NotEmpty().WithMessage("Método de pagamento é obrigatório.");

        RuleFor(x => x.EntradaMinimaPercentual)
            .InclusiveBetween(0, 100).WithMessage("Entrada mínima percentual deve estar entre 0 e 100.");

        RuleFor(x => x.DescontoPercentual)
            .InclusiveBetween(0, 100).WithMessage("Desconto percentual deve estar entre 0 e 100.");

        RuleFor(x => x.AcrescimoPercentual)
            .GreaterThanOrEqualTo(0).WithMessage("Acréscimo percentual não pode ser negativo.");

        RuleFor(x => x.MultaPercentual)
            .GreaterThanOrEqualTo(0).WithMessage("Multa percentual não pode ser negativa.");

        RuleFor(x => x.TaxaJurosPercentual)
            .GreaterThanOrEqualTo(0).WithMessage("Taxa de juros percentual não pode ser negativa.");

        RuleFor(x => x.Parcelas)
            .NotEmpty().When(x => x.EntradaMinimaPercentual < 100).WithMessage("Condição de pagamento deve conter ao menos uma parcela.");

        RuleForEach(x => x.Parcelas).ChildRules(p =>
        {
            p.RuleFor(x => x.NumeroParcela)
                .GreaterThan(0).WithMessage("Número da parcela deve ser maior que zero.");

            p.RuleFor(x => x.Percentual)
                .GreaterThan(0).WithMessage("Percentual da parcela deve ser maior que zero.")
                .LessThanOrEqualTo(100).WithMessage("Percentual da parcela não pode exceder 100%.");

            p.RuleFor(x => x.PrazoDias)
                .GreaterThanOrEqualTo(0).WithMessage("Prazo em dias não pode ser negativo.");
        }).When(x => x.EntradaMinimaPercentual < 100);

        RuleFor(x => x).Custom((cmd, context) =>
        {
            if (cmd.DescontoPercentual > 0 && cmd.AcrescimoPercentual > 0)
            {
                context.AddFailure("DescontoPercentual", "Não é permitido definir desconto e acréscimo simultaneamente.");
                context.AddFailure("AcrescimoPercentual", "Não é permitido definir desconto e acréscimo simultaneamente.");
            }

            if (cmd.EntradaMinimaPercentual == 100)
            {
                if (cmd.Parcelas != null && cmd.Parcelas.Any())
                {
                    context.AddFailure("Parcelas", "Uma condição de pagamento à vista não deve possuir parcelas.");
                }
                return;
            }

            if (cmd.Parcelas == null || !cmd.Parcelas.Any())
            {
                context.AddFailure("Parcelas", "Condição de pagamento a prazo deve conter ao menos uma parcela.");
                return;
            }

            var totalPercentual = cmd.Parcelas.Sum(p => p.Percentual);
            if (totalPercentual <= 0)
            {
                context.AddFailure("Parcelas", "O percentual total das parcelas deve ser maior que zero.");
            }
            if (cmd.EntradaMinimaPercentual + totalPercentual != 100)
            {
                context.AddFailure("Parcelas", $"A soma da entrada mínima ({cmd.EntradaMinimaPercentual}%) e das parcelas ({totalPercentual}%) deve ser exatamente igual a 100%. Atual: {cmd.EntradaMinimaPercentual + totalPercentual}%.");
            }

            var duplicateNumbers = cmd.Parcelas.GroupBy(p => p.NumeroParcela).Where(g => g.Count() > 1);
            if (duplicateNumbers.Any())
            {
                context.AddFailure("Parcelas", "Não pode haver parcelas com números repetidos.");
            }
        });
    }
}
