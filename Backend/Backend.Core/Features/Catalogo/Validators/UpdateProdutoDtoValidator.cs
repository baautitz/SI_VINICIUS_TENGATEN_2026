using Backend.Core.Features.Catalogo.DTOs;
using FluentValidation;

namespace Backend.Core.Features.Catalogo.Validators;

public sealed class UpdateProdutoDtoValidator : AbstractValidator<UpdateProdutoDto>
{
    public UpdateProdutoDtoValidator()
    {
        RuleFor(x => x.Produto)
            .NotEmpty().WithMessage("Nome do produto é obrigatório.")
            .WithErrorCode("PRODUTO_OBRIGATORIO")
            .MaximumLength(150).WithMessage("Nome do produto deve ter no máximo 150 caracteres.")
            .WithErrorCode("PRODUTO_TAMANHO_INVALIDO");



        RuleFor(x => x.CategoriaId)
            .GreaterThan(0).WithMessage("Categoria é obrigatória.")
            .WithErrorCode("CATEGORIA_OBRIGATORIA");

        RuleFor(x => x.MarcaId)
            .GreaterThan(0).WithMessage("Marca é obrigatória.")
            .WithErrorCode("MARCA_OBRIGATORIA");

        RuleFor(x => x.UnidadeMedidaId)
            .GreaterThan(0).WithMessage("Unidade de medida é obrigatória.")
            .WithErrorCode("UNIDADE_MEDIDA_OBRIGATORIA");

        RuleFor(x => x.Skus)
            .NotEmpty().WithMessage("O produto deve possuir pelo menos um SKU.")
            .WithErrorCode("SKUS_OBRIGATORIOS")
            .Must(skus => skus == null || skus.Select(s => s.Sku?.Trim().ToUpperInvariant()).Distinct().Count() == skus.Count)
            .WithMessage("Não podem existir códigos SKU duplicados no mesmo produto.")
            .WithErrorCode("SKU_DUPLICADO");

        RuleForEach(x => x.Skus).SetValidator(new UpdateSkuDtoValidator());
    }
}

public sealed class UpdateSkuDtoValidator : AbstractValidator<UpdateSkuDto>
{
    public UpdateSkuDtoValidator()
    {
        RuleFor(x => x.Sku)
            .NotEmpty().WithMessage("Código SKU é obrigatório.")
            .WithErrorCode("SKU_OBRIGATORIO")
            .MaximumLength(50).WithMessage("SKU deve ter no máximo 50 caracteres.")
            .WithErrorCode("SKU_TAMANHO_INVALIDO");

        RuleFor(x => x.Preco)
            .GreaterThanOrEqualTo(0).WithMessage("Preço não pode ser negativo.")
            .WithErrorCode("PRECO_INVALIDO");

        RuleFor(x => x.Estoque)
            .GreaterThanOrEqualTo(0).WithMessage("Estoque não pode ser negativo.")
            .WithErrorCode("ESTOQUE_INVALIDO");

        RuleFor(x => x.GtinEan)
            .MaximumLength(14).WithMessage("Código de barras deve ter no máximo 14 caracteres.")
            .WithErrorCode("GTIN_EAN_TAMANHO_INVALIDO");
    }
}
