using Backend.Core.Features.Logistica.Commands;
using FluentValidation;

namespace Backend.Core.Features.Logistica.Validators.Commands;

public class AtualizarVeiculoCommandValidator : AbstractValidator<AtualizarVeiculoCommand>
{
    public AtualizarVeiculoCommandValidator()
    {
        RuleFor(x => x.Placa)
            .NotEmpty().WithMessage("Placa é obrigatória.")
            .MaximumLength(10).WithMessage("Placa deve ter no máximo 10 caracteres.");

        RuleFor(x => x.EstadoId)
            .GreaterThan(0).WithMessage("Estado é obrigatório.");

        RuleFor(x => x.TransportadoraId)
            .GreaterThan(0).When(x => x.TransportadoraId.HasValue).WithMessage("Transportadora inválida.");

        RuleFor(x => x.MarcaModelo)
            .MaximumLength(100).WithMessage("Marca/Modelo deve ter no máximo 100 caracteres.");

        RuleFor(x => x.Rntrc)
            .MaximumLength(20).WithMessage("RNTRC deve ter no máximo 20 caracteres.");

        RuleFor(x => x.Renavam)
            .MaximumLength(20).WithMessage("Renavam deve ter no máximo 20 caracteres.");

        RuleFor(x => x.TipoVeiculo)
            .MaximumLength(50).WithMessage("Tipo de veículo deve ter no máximo 50 caracteres.");

        RuleFor(x => x.Observacao)
            .MaximumLength(500).WithMessage("Observação deve ter no máximo 500 caracteres.");
    }
}
