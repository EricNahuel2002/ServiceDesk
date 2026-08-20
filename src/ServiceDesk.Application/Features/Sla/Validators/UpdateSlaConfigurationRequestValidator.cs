using FluentValidation;
using ServiceDesk.Application.DTOs.Sla;

namespace ServiceDesk.Application.Features.Sla.Validators;

public sealed class UpdateSlaConfigurationRequestValidator : AbstractValidator<UpdateSlaConfigurationRequest>
{
    public UpdateSlaConfigurationRequestValidator()
    {
        RuleFor(x => x.Priority)
            .IsInEnum()
            .WithMessage("La prioridad indicada no es válida.");

        RuleFor(x => x.ResponseTimeHours)
            .GreaterThan(0)
            .WithMessage("El tiempo de respuesta debe ser mayor a 0 horas.")
            .LessThanOrEqualTo(168)
            .WithMessage("El tiempo de respuesta no puede exceder 168 horas (7 días).");
    }
}
