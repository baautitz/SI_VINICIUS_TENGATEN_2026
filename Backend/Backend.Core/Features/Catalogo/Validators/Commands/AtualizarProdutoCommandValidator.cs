using Backend.Core.Features.Catalogo.Commands;
using FluentValidation;

namespace Backend.Core.Features.Catalogo.Validators.Commands;

public class AtualizarProdutoCommandValidator : AbstractValidator<AtualizarProdutoCommand>
{
    public AtualizarProdutoCommandValidator()
    {
        RuleFor(x => x.Produto).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CategoriaId).GreaterThan(0);
        RuleFor(x => x.MarcaId).GreaterThan(0);
        RuleFor(x => x.UnidadeMedidaId).GreaterThan(0);
        RuleForEach(x => x.Skus).ChildRules(sku =>
        {
            sku.RuleFor(s => s.Sku).MaximumLength(50);
            sku.RuleFor(s => s.Preco).GreaterThanOrEqualTo(0);
        });
    }
}
