using Backend.Core.Features.Estoque.Commands;
using Backend.Core.Features.Estoque.Entities.Enums;
using FluentValidation;

namespace Backend.Core.Features.Estoque.Validators.Commands;

public sealed class CriarMovimentacaoCommandValidator : AbstractValidator<CriarMovimentacaoCommand>
{
    public CriarMovimentacaoCommandValidator()
    {
        // Não existe entrada manual: a entrada vem da Compra e dos retornos (ADR 0006).
        RuleFor(x => x.MotivoTipo)
            .NotEmpty().WithMessage("Motivo da movimentação é obrigatório.")
            .WithErrorCode("MOTIVO_TIPO_OBRIGATORIO")
            .IsEnumName(typeof(MotivoMovimentacaoEstoque), caseSensitive: false)
            .WithMessage("Motivo da movimentação inválido.")
            .WithErrorCode("MOTIVO_TIPO_INVALIDO")
            .Must(m => !System.Enum.TryParse<MotivoMovimentacaoEstoque>(m, true, out var motivo) || motivo.GeraNfeDeBaixa())
            .WithMessage("Toda saída manual exige NF-e de baixa: use perda/avaria, furto/roubo ou vencimento. Erro de contagem e outros só existem no balanço.")
            .WithErrorCode("MOTIVO_TIPO_INVALIDO");

        RuleFor(x => x.EmitenteId)
            .NotNull().WithMessage("Informe o emitente da NF-e de baixa.")
            .WithErrorCode("EMITENTE_OBRIGATORIO")
            .When(x => System.Enum.TryParse<MotivoMovimentacaoEstoque>(x.MotivoTipo, true, out var motivo) && motivo.GeraNfeDeBaixa());

        RuleFor(x => x.Motivo)
            .NotEmpty().WithMessage("Motivo é obrigatório.")
            .WithErrorCode("MOTIVO_OBRIGATORIO")
            .MinimumLength(5).WithMessage("Motivo deve ter pelo menos 5 caracteres.")
            .WithErrorCode("MOTIVO_INVALIDO")
            .MaximumLength(500).WithMessage("Motivo deve ter no máximo 500 caracteres.")
            .WithErrorCode("MOTIVO_INVALIDO");


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
