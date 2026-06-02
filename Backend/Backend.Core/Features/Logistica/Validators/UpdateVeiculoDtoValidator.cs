using Backend.Core.Features.Logistica.DTOs;
using FluentValidation;

namespace Backend.Core.Features.Logistica.Validators;

public class UpdateVeiculoDtoValidator : AbstractValidator<UpdateVeiculoDto>
{
    public UpdateVeiculoDtoValidator()
    {
        RuleFor(x => x.Placa)
            .NotEmpty().WithMessage("Placa é obrigatória.")
            .MaximumLength(10).WithMessage("Placa deve ter no máximo 10 caracteres.");

        RuleFor(x => x.EstadoId)
            .GreaterThan(0).WithMessage("Estado é obrigatório.");
            
        RuleFor(x => x.Renavam)
            .MaximumLength(20).WithMessage("Renavam deve ter no máximo 20 caracteres.");
            
        RuleFor(x => x.Rntrc)
            .MaximumLength(20).WithMessage("RNTRC deve ter no máximo 20 caracteres.");
    }
}
