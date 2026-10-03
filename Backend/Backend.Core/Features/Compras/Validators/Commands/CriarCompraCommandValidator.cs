using Backend.Core.Features.Compras.Commands;
using FluentValidation;

namespace Backend.Core.Features.Compras.Validators.Commands;

public class CriarCompraCommandValidator : AbstractValidator<CriarCompraCommand>
{
    public CriarCompraCommandValidator()
    {
        RuleFor(x => x.DataCompra).NotEmpty().WithMessage("Data da compra é obrigatória.");
        RuleFor(x => x.EmitenteId).GreaterThan(0).WithMessage("Emitente é obrigatório.");
        RuleFor(x => x.FornecedorId).GreaterThan(0).WithMessage("Fornecedor é obrigatório.");
        RuleFor(x => x.ValorFrete).GreaterThanOrEqualTo(0).WithMessage("Frete não pode ser negativo.");
        RuleFor(x => x.ValorSeguro).GreaterThanOrEqualTo(0).WithMessage("Seguro não pode ser negativo.");
        RuleFor(x => x.ValorOutrasDespesas).GreaterThanOrEqualTo(0).WithMessage("Outras despesas não podem ser negativas.");
        RuleFor(x => x.ValorDesconto).GreaterThanOrEqualTo(0).WithMessage("Desconto não pode ser negativo.");
        RuleFor(x => x.ValorFreteTerceiro).GreaterThanOrEqualTo(0).WithMessage("Frete de terceiro não pode ser negativo.");

        RuleFor(x => x.TransportadoraId).NotNull().WithMessage("Frete de terceiro exige a transportadora.")
            .When(x => x.ValorFreteTerceiro > 0);
        RuleFor(x => x.VencimentoFreteTerceiro).NotNull().WithMessage("Frete de terceiro exige a data de vencimento.")
            .When(x => x.ValorFreteTerceiro > 0);

        RuleFor(x => x.Itens).NotEmpty().WithMessage("A compra deve conter ao menos um item.");
        RuleForEach(x => x.Itens).ChildRules(item =>
        {
            item.RuleFor(i => i.Sku).NotEmpty().WithMessage("SKU do produto é obrigatório.");
            item.RuleFor(i => i.Quantidade).GreaterThan(0).WithMessage("Quantidade do item deve ser maior que zero.");
            item.RuleFor(i => i.ValorUnitario).GreaterThan(0).WithMessage("Valor unitário do item deve ser maior que zero.");
            item.RuleFor(i => i).Must(i => i.ValorDesconto >= 0 && i.ValorDesconto <= i.Quantidade * i.ValorUnitario)
                .WithMessage("O desconto do item deve estar entre zero e o valor do item.");
        });

        RuleFor(x => x.Parcelas).NotEmpty().WithMessage("As parcelas devem ser informadas.");
        RuleForEach(x => x.Parcelas).ChildRules(p =>
        {
            p.RuleFor(x => x.NumeroParcela).GreaterThan(0).WithMessage("Número da parcela deve ser maior que zero.");
            p.RuleFor(x => x.DataVencimento).NotEmpty().WithMessage("Data de vencimento da parcela é obrigatória.");
            p.RuleFor(x => x.ValorParcela).GreaterThan(0).WithMessage("Valor da parcela deve ser maior que zero.").WithErrorCode("PARCELA_VALOR_INVALIDO");
        }).When(x => x.Parcelas != null);

        // valor_nota = produtos + frete + seguro + outras - desconto (sem frete de terceiro).
        RuleFor(x => x)
            .Must(x => Math.Abs(x.Parcelas!.Sum(p => p.ValorParcela)
                - (x.Itens.Sum(i => i.Quantidade * i.ValorUnitario - i.ValorDesconto) + x.ValorFrete + x.ValorSeguro + x.ValorOutrasDespesas - x.ValorDesconto)) < 0.005m)
            .WithMessage("A soma das parcelas deve ser igual ao valor da nota.")
            .When(x => x.Itens != null && x.Itens.Any() && x.Parcelas != null);
    }
}
