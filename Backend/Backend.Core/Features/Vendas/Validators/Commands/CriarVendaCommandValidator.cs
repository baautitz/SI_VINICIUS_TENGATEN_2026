using System;
using System.Linq;
using Backend.Core.Features.Vendas.Commands;
using FluentValidation;

namespace Backend.Core.Features.Vendas.Validators.Commands;

public class CriarVendaCommandValidator : AbstractValidator<CriarVendaCommand>
{
    public CriarVendaCommandValidator()
    {
        RuleFor(x => x.DataVenda)
            .NotEmpty().WithMessage("Data da venda é obrigatória.");

        RuleFor(x => x.EmitenteId)
            .GreaterThan(0).WithMessage("Emitente é obrigatório.");

        RuleFor(x => x.ClienteId)
            .GreaterThan(0).WithMessage("Cliente é obrigatório.");

        RuleFor(x => x.Itens)
            .NotEmpty().WithMessage("A venda deve conter ao menos um item.");

        RuleForEach(x => x.Itens).ChildRules(item =>
        {
            item.RuleFor(i => i.Sku)
                .NotEmpty().WithMessage("SKU do produto é obrigatório.");

            item.RuleFor(i => i.Quantidade)
                .GreaterThan(0).WithMessage("Quantidade do item deve ser maior que zero.");

            item.RuleFor(i => i.ValorUnitario)
                .GreaterThanOrEqualTo(0).WithMessage("Valor unitário do item não pode ser negativo.");

            item.RuleFor(i => i.ValorDesconto)
                .GreaterThanOrEqualTo(0).WithMessage("Desconto do item não pode ser negativo.");

            item.RuleFor(i => i)
                .Must(i => i.ValorDesconto <= i.Quantidade * i.ValorUnitario)
                .WithMessage("O desconto não pode ser maior que o valor total do item (Quantidade * Valor Unitário).");
        });

        RuleFor(x => x.CondicaoPagamentoId)
            .NotEmpty().WithMessage("Condição de Pagamento é obrigatória.");

        RuleFor(x => x.Parcelas)
            .NotEmpty().WithMessage("As parcelas devem ser informadas para a condição de pagamento.");

        RuleForEach(x => x.Parcelas).ChildRules(parcela =>
        {
            parcela.RuleFor(p => p.NumeroParcela)
                .GreaterThan(0).WithMessage("Número da parcela deve ser maior que zero.");

            parcela.RuleFor(p => p.DataVencimento)
                .NotEmpty().WithMessage("Data de vencimento da parcela é obrigatória.");

            parcela.RuleFor(p => p.ValorParcela)
                .GreaterThan(0).WithMessage("Valor da parcela deve ser maior que zero.");
        }).When(x => x.Parcelas != null);

        RuleFor(x => x)
            .Must(x => x.Parcelas != null && Math.Abs(x.Parcelas.Sum(p => p.ValorParcela) - x.Itens.Sum(i => i.Quantidade * i.ValorUnitario - i.ValorDesconto)) < 0.01m)
            .WithMessage("A soma dos valores das parcelas deve ser exatamente igual ao valor total da venda.")
            .When(x => x.Itens != null && x.Itens.Any() && x.Parcelas != null);
    }
}
