using Backend.Core.Features.Estoque.Commands;
using Backend.Core.Features.Estoque.Entities.Enums;
using FluentValidation;
using System;

namespace Backend.Core.Features.Estoque.Validators.Commands;

public sealed class AtualizarMovimentacaoCommandValidator : AbstractValidator<AtualizarMovimentacaoCommand>
{
    public AtualizarMovimentacaoCommandValidator()
    {
        RuleFor(x => x.TipoMovimentacao)
            .NotEmpty().WithMessage("Tipo de movimentação é obrigatório.")
            .WithErrorCode("TIPO_MOVIMENTACAO_OBRIGATORIO")
            .IsEnumName(typeof(TipoMovimentacaoEstoque), caseSensitive: false)
            .WithMessage("Tipo de movimentação inválido.")
            .WithErrorCode("TIPO_MOVIMENTACAO_INVALIDO");

        RuleFor(x => x.Observacao)
            .MaximumLength(500).WithMessage("Observação deve ter no máximo 500 caracteres.")
            .WithErrorCode("OBSERVACAO_EXCEDE_LIMITE");

        RuleFor(x => x.Itens)
            .NotEmpty().WithMessage("A movimentação deve conter pelo menos um item.")
            .WithErrorCode("ITENS_OBRIGATORIOS");

        RuleForEach(x => x.Itens).ChildRules(item =>
        {
            item.RuleFor(i => i.Sku)
                .NotEmpty().WithMessage("SKU do item é obrigatório.")
                .WithErrorCode("SKU_OBRIGATORIO");

            item.RuleFor(i => i.Quantidade)
                .GreaterThan(0).WithMessage("Quantidade deve ser maior que zero.")
                .WithErrorCode("QUANTIDADE_MINIMA");

            item.RuleFor(i => i.CustoUnitario)
                .GreaterThanOrEqualTo(0).WithMessage("Custo unitário não pode ser negativo.")
                .WithErrorCode("CUSTO_MINIMO");
        });
    }
}
