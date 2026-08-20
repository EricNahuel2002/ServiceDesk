using FluentValidation;
using ServiceDesk.Application.DTOs.Tickets;

namespace ServiceDesk.Application.Features.Tickets.Validators;

public sealed class UpdateTicketRequestValidator : AbstractValidator<UpdateTicketRequest>
{
    public UpdateTicketRequestValidator()
    {
        RuleFor(x => x.AssignedToId)
            .NotEmpty()
            .When(x => x.AssignedToId.HasValue)
            .WithMessage("El técnico asignado no puede ser un GUID vacío.");

        RuleFor(x => x.Priority)
            .IsInEnum()
            .When(x => x.Priority.HasValue)
            .WithMessage("La prioridad indicada no es válida.");

        RuleFor(x => x)
            .Must(x => x.AssignedToId.HasValue || x.Priority.HasValue)
            .WithMessage("Debe indicar al menos un campo para actualizar (AssignedToId o Priority).");
    }
}
