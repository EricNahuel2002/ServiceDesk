using FluentValidation;
using ServiceDesk.Application.DTOs.Sla;

namespace ServiceDesk.Application.Features.Sla.Validators;

public sealed class UpdateBusinessHoursRequestValidator : AbstractValidator<UpdateBusinessHoursRequest>
{
    public UpdateBusinessHoursRequestValidator()
    {
        RuleFor(x => x.TimeZoneId)
            .NotEmpty()
            .WithMessage("La zona horaria es obligatoria.")
            .MaximumLength(100)
            .WithMessage("La zona horaria no puede exceder 100 caracteres.");

        RuleFor(x => x.BusinessHoursJson)
            .NotEmpty()
            .WithMessage("Los horarios de trabajo son obligatorios.")
            .MaximumLength(4000)
            .WithMessage("Los horarios de trabajo no pueden exceder 4000 caracteres.");
    }
}
